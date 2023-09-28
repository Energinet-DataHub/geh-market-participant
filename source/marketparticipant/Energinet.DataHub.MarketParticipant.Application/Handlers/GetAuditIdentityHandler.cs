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
using System.Collections.Generic;
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
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers;

public sealed class GetAuditIdentityHandler : IRequestHandler<GetAuditIdentityCommand, GetAuditIdentityResponse>
{
    private static readonly IReadOnlyDictionary<AuditIdentity, KnownAuditIdentityProvider> _knownAuditIdentities
        = new Dictionary<AuditIdentity, KnownAuditIdentityProvider>
        {
            { KnownAuditIdentityProvider.Migration.IdentityId, KnownAuditIdentityProvider.Migration },
            { KnownAuditIdentityProvider.TestFramework.IdentityId, KnownAuditIdentityProvider.TestFramework },
            { KnownAuditIdentityProvider.OrganizationBackgroundService.IdentityId, KnownAuditIdentityProvider.OrganizationBackgroundService },
        };

    private readonly IUserRepository _userRepository;
    private readonly IActorRepository _actorRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IUserIdentityRepository _userIdentityRepository;
    private readonly IUserContext<FrontendUser> _userContext;

    public GetAuditIdentityHandler(
        IUserRepository userRepository,
        IActorRepository actorRepository,
        IOrganizationRepository organizationRepository,
        IUserIdentityRepository userIdentityRepository,
        IUserContext<FrontendUser> userContext)
    {
        _userRepository = userRepository;
        _actorRepository = actorRepository;
        _organizationRepository = organizationRepository;
        _userIdentityRepository = userIdentityRepository;
        _userContext = userContext;
    }

    public async Task<GetAuditIdentityResponse> Handle(GetAuditIdentityCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // NOTE: This handler provides audit identity information without requiring explicit permissions, like users:view.
        // The implementation is therefore required to check the current user context and filter the results.
        // - If user is FAS or belongs to organization, we can return all audit identity information.
        // - If user do not belong to the organization, we can only return audit identity organization name.
        var auditIdentity = new AuditIdentity(request.AuditIdentityId);

        if (_knownAuditIdentities.TryGetValue(auditIdentity, out var knownAuditIdentity))
        {
            return _userContext.CurrentUser.IsFas
                ? new GetAuditIdentityResponse($"DataHub ({knownAuditIdentity.FriendlyName})")
                : new GetAuditIdentityResponse("DataHub");
        }

        var user = await _userRepository
            .GetAsync(new UserId(auditIdentity.Value))
            .ConfigureAwait(false);

        NotFoundValidationException.ThrowIfNull(user, request.AuditIdentityId);

        var showOrganizationOnly = !HasCurrentUserAccessToUser(user);
        if (showOrganizationOnly)
        {
            var actor = await _actorRepository
                .GetAsync(user.AdministratedBy)
                .ConfigureAwait(false) ?? throw new InvalidOperationException($"Actor for user {user.Id} not found.");

            var organization = await _organizationRepository
                .GetAsync(actor.OrganizationId)
                .ConfigureAwait(false);

            return organization != null
                ? new GetAuditIdentityResponse(organization.Name)
                : throw new InvalidOperationException($"Organization for actor {actor.Id} not found.");
        }

        var userIdentity = await _userIdentityRepository
            .GetAsync(user.ExternalId)
            .ConfigureAwait(false);

        NotFoundValidationException.ThrowIfNull(userIdentity, $"No external identity found for id {request.AuditIdentityId}.");

        return new GetAuditIdentityResponse($"{userIdentity.FirstName} ({userIdentity.Email})");
    }

    private bool HasCurrentUserAccessToUser(Domain.Model.Users.User user)
    {
        return _userContext.CurrentUser.IsFas ||
               _userContext.CurrentUser.IsAssignedToActor(user.AdministratedBy.Value) ||
               user.RoleAssignments
                   .Select(ura => ura.ActorId.Value)
                   .Any(_userContext.CurrentUser.IsAssignedToActor);
    }
}
