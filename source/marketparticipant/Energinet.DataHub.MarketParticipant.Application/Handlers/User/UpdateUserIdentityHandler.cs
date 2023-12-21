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
using Energinet.DataHub.MarketParticipant.Application.Commands.User;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.User;

public sealed class UpdateUserIdentityHandler : IRequestHandler<UpdateUserIdentityCommand>
{
    private readonly IAuditIdentityProvider _auditIdentityProvider;
    private readonly IUserRepository _userRepository;
    private readonly IUserIdentityRepository _userIdentityRepository;
    private readonly IUserIdentityAuditLogRepository _userIdentityAuditLogRepository;
    private readonly IUnitOfWorkProvider _unitOfWorkProvider;

    public UpdateUserIdentityHandler(
        IAuditIdentityProvider auditIdentityProvider,
        IUserRepository userRepository,
        IUserIdentityRepository userIdentityRepository,
        IUserIdentityAuditLogRepository userIdentityAuditLogRepository,
        IUnitOfWorkProvider unitOfWorkProvider)
    {
        _auditIdentityProvider = auditIdentityProvider;
        _userRepository = userRepository;
        _userIdentityRepository = userIdentityRepository;
        _userIdentityAuditLogRepository = userIdentityAuditLogRepository;
        _unitOfWorkProvider = unitOfWorkProvider;
    }

    public async Task Handle(UpdateUserIdentityCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await _userRepository.GetAsync(new UserId(request.UserId)).ConfigureAwait(false);
        NotFoundValidationException.ThrowIfNull(user, request.UserId, $"The specified user {request.UserId} was not found.");

        var userIdentity = await _userIdentityRepository.GetAsync(user.ExternalId).ConfigureAwait(false);
        NotFoundValidationException.ThrowIfNull(userIdentity, user.ExternalId.Value, $"The specified user identity {user.ExternalId} was not found.");

        var uow = await _unitOfWorkProvider
            .NewUnitOfWorkAsync()
            .ConfigureAwait(false);

        await using (uow.ConfigureAwait(false))
        {
            await AuditWhenChangedAsync(
                user.Id,
                UserAuditedChange.FirstName,
                request.UserIdentityUpdate.FirstName,
                userIdentity.FirstName).ConfigureAwait(false);

            await AuditWhenChangedAsync(
                user.Id,
                UserAuditedChange.LastName,
                request.UserIdentityUpdate.LastName,
                userIdentity.LastName).ConfigureAwait(false);

            await AuditWhenChangedAsync(
                user.Id,
                UserAuditedChange.PhoneNumber,
                request.UserIdentityUpdate.PhoneNumber,
                userIdentity.PhoneNumber?.Number).ConfigureAwait(false);

            userIdentity.PhoneNumber = new PhoneNumber(request.UserIdentityUpdate.PhoneNumber);
            userIdentity.LastName = request.UserIdentityUpdate.LastName;
            userIdentity.FirstName = request.UserIdentityUpdate.FirstName;

            await _userIdentityRepository.UpdateUserAsync(userIdentity).ConfigureAwait(false);

            await uow.CommitAsync().ConfigureAwait(false);
        }
    }

    private Task AuditWhenChangedAsync(UserId userId, UserAuditedChange change, string? currentValue, string? previousValue)
    {
        if (currentValue == previousValue)
            return Task.CompletedTask;

        return _userIdentityAuditLogRepository.AuditAsync(
            userId,
            _auditIdentityProvider.IdentityId,
            change,
            currentValue,
            previousValue);
    }
}
