// Copyright 2020 Energinet DataHub A/S
//
// Licensed under the Apache License, Version 2.0 (the "License2");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Users;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.RevisionLog.Integration;
using MediatR;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Users;

public sealed class UserInvitationExpiredHandler : IRequestHandler<UserInvitationExpiredCommand>
{
    private readonly IUnitOfWorkProvider _unitOfWorkProvider;
    private readonly IEntityLock _entityLock;
    private readonly IUserRepository _userRepository;
    private readonly IUserIdentityRepository _userIdentityRepository;
    private readonly IUserIdentityAuditLogRepository _userIdentityAuditLogRepository;
    private readonly IAuditIdentityProvider _auditIdentityProvider;
    private readonly ILogger<UserInvitationExpiredHandler> _logger;
    private readonly IRevisionLogClient _revisionLogClient;

    public UserInvitationExpiredHandler(
        IUnitOfWorkProvider unitOfWorkProvider,
        IEntityLock entityLock,
        IUserRepository userRepository,
        IUserIdentityRepository userIdentityRepository,
        IUserIdentityAuditLogRepository userIdentityAuditLogRepository,
        IAuditIdentityProvider auditIdentityProvider,
        ILogger<UserInvitationExpiredHandler> logger,
        IRevisionLogClient revisionLogClient)
    {
        _unitOfWorkProvider = unitOfWorkProvider;
        _entityLock = entityLock;
        _userRepository = userRepository;
        _userIdentityRepository = userIdentityRepository;
        _userIdentityAuditLogRepository = userIdentityAuditLogRepository;
        _auditIdentityProvider = auditIdentityProvider;
        _logger = logger;
        _revisionLogClient = revisionLogClient;
    }

    public async Task Handle(UserInvitationExpiredCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var usersWithExpiredInvitation = await _userRepository.FindUsersWithExpiredInvitationAsync().ConfigureAwait(false);

        foreach (var user in usersWithExpiredInvitation)
        {
            var unitOfWork = await _unitOfWorkProvider.NewUnitOfWorkAsync().ConfigureAwait(false);

            await using (unitOfWork.ConfigureAwait(false))
            {
                try
                {
                    var userIdentity = await _userIdentityRepository
                        .GetAsync(user.ExternalId)
                        .ConfigureAwait(false);

                    if (userIdentity!.Status == UserIdentityStatus.Inactive)
                    {
                        continue;
                    }

                    await _entityLock.LockAsync(LockableEntity.User).ConfigureAwait(false);

                    await _userIdentityRepository
                        .DisableUserAccountAsync(userIdentity)
                        .ConfigureAwait(false);

                    await _userIdentityAuditLogRepository
                        .AuditAsync(
                            user.Id,
                            _auditIdentityProvider.IdentityId,
                            UserAuditedChange.Status,
                            UserStatus.Inactive.ToString(),
                            userIdentity.Status.ToString())
                        .ConfigureAwait(false);

                    await _revisionLogClient.LogAsync(new RevisionLogEntry(
                        logId: Guid.NewGuid(),
                        systemId: SubsystemInformation.Id,
                        activity: "DeactivateUserWithExpiredInvitation",
                        occurredOn: SystemClock.Instance.GetCurrentInstant(),
                        origin: "UserInvitationExpired",
                        affectedEntityType: nameof(UserIdentity),
                        affectedEntityKey: user.ExternalId.ToString(),
                        payload: string.Empty)).ConfigureAwait(false);

                    _logger.LogInformation("User identity disabled for user with external id {ExternalId}", user.ExternalId);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "An error occured when trying to deactivate user identity with external id {ExternalId}", user.ExternalId);
                    throw;
                }

                await unitOfWork.CommitAsync().ConfigureAwait(false);
            }
        }
    }
}
