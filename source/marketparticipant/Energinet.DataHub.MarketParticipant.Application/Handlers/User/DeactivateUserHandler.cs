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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.Core.App.Common.Abstractions.Users;
using Energinet.DataHub.MarketParticipant.Application.Commands.User;
using Energinet.DataHub.MarketParticipant.Application.Security;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.User;

public sealed class DeactivateUserHandler : IRequestHandler<DeactivateUserCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserIdentityRepository _userIdentityRepository;
    private readonly IUserStatusCalculator _userStatusCalculator;
    private readonly IUserIdentityAuditLogRepository _userIdentityAuditLogRepository;
    private readonly IAuditIdentityProvider _auditIdentityProvider;
    private readonly IUserContext<FrontendUser> _userContext;

    public DeactivateUserHandler(
        IUserRepository userRepository,
        IUserIdentityRepository userIdentityRepository,
        IUserStatusCalculator userStatusCalculator,
        IUserIdentityAuditLogRepository userIdentityAuditLogRepository,
        IAuditIdentityProvider auditIdentityProvider,
        IUserContext<FrontendUser> userContext)
    {
        _userRepository = userRepository;
        _userIdentityRepository = userIdentityRepository;
        _userStatusCalculator = userStatusCalculator;
        _userIdentityAuditLogRepository = userIdentityAuditLogRepository;
        _auditIdentityProvider = auditIdentityProvider;
        _userContext = userContext;
    }

    public async Task Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await _userRepository.GetAsync(new UserId(request.UserId)).ConfigureAwait(false);
        NotFoundValidationException.ThrowIfNull(user, request.UserId);

        await (request.IsAdministratedByIdentity switch
        {
            true => DeactivateUserAsync(user),
            false => RemoveUserFromCurrentActorAsync(user),
        }).ConfigureAwait(false);
    }

    private async Task DeactivateUserAsync(Domain.Model.Users.User user)
    {
        var userIdentity = await _userIdentityRepository.GetAsync(user.ExternalId).ConfigureAwait(false);
        NotFoundValidationException.ThrowIfNull(userIdentity, user.ExternalId.Value);

        var currentStatus = _userStatusCalculator.CalculateUserStatus(user, userIdentity);
        if (currentStatus == UserStatus.Inactive)
            return;

        await _userIdentityRepository
            .DisableUserAccountAsync(userIdentity.Id)
            .ConfigureAwait(false);

        if (user.InvitationExpiresAt.HasValue)
        {
            user.DeactivateUserExpiration();

            await _userRepository
                .AddOrUpdateAsync(user)
                .ConfigureAwait(false);
        }

        await _userIdentityAuditLogRepository
            .AuditAsync(
                user.Id,
                _auditIdentityProvider.IdentityId,
                UserAuditedChange.Status,
                UserStatus.Inactive.ToString(),
                currentStatus.ToString())
            .ConfigureAwait(false);
    }

    private async Task RemoveUserFromCurrentActorAsync(Domain.Model.Users.User user)
    {
        var userRoleAssignments = user.RoleAssignments.Where(x => x.ActorId.Value == _userContext.CurrentUser.ActorId);

        foreach (var userRoleAssignment in userRoleAssignments)
        {
            user.RoleAssignments.Remove(userRoleAssignment);
        }

        await _userRepository
            .AddOrUpdateAsync(user)
            .ConfigureAwait(false);
    }
}
