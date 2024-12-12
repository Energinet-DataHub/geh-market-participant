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
using Energinet.DataHub.MarketParticipant.Application.Commands.Users;
using Energinet.DataHub.MarketParticipant.Application.Security;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
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
            { KnownAuditIdentityProvider.ProcessManagerBackgroundJobs.IdentityId, KnownAuditIdentityProvider.ProcessManagerBackgroundJobs },
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
        var auditIdentities = request.AuditIdentities.Select(id => new AuditIdentity(id));
        var auditIdentityDisplayNames = await HandleKnownAuditIdentitiesOrContinueAsync(auditIdentities).ConfigureAwait(false);

        return new GetAuditIdentityResponse(auditIdentityDisplayNames);
    }

    private async Task<IEnumerable<AuditIdentityDto>> HandleKnownAuditIdentitiesOrContinueAsync(IEnumerable<AuditIdentity> auditIdentities)
    {
        var handledIdentities = new List<AuditIdentityDto>();
        var nextIdentities = new List<UserId>();

        foreach (var auditIdentity in auditIdentities)
        {
            if (_knownAuditIdentities.TryGetValue(auditIdentity, out var knownAuditIdentity))
            {
                handledIdentities.Add(_userContext.CurrentUser.IsFas
                    ? new AuditIdentityDto(auditIdentity.Value, $"DataHub ({knownAuditIdentity.FriendlyName})")
                    : new AuditIdentityDto(auditIdentity.Value, "DataHub"));
            }
            else
            {
                nextIdentities.Add(new UserId(auditIdentity.Value));
            }
        }

        var nextProcessing = await HandleOrganizationNamesOrContinueAsync(nextIdentities).ConfigureAwait(false);
        return handledIdentities.Concat(nextProcessing);
    }

    private async Task<IEnumerable<AuditIdentityDto>> HandleOrganizationNamesOrContinueAsync(IReadOnlyCollection<UserId> userIds)
    {
        var handledIdentities = new List<AuditIdentityDto>();
        var nextIdentities = new List<User>();

        var users = await _userRepository
            .GetAsync(userIds)
            .ConfigureAwait(false);

        var lookup = users.ToDictionary(user => user.Id);
        var organizationNames = new Dictionary<ActorId, string>();

        foreach (var userId in userIds)
        {
            var user = lookup.GetValueOrDefault(userId);
            NotFoundValidationException.ThrowIfNull(user, userId.Value);

            var showOrganizationOnly = !HasCurrentUserAccessToActor(user);
            if (showOrganizationOnly)
            {
                if (!organizationNames.TryGetValue(user.AdministratedBy, out var organizationName))
                {
                    var actor = await _actorRepository
                        .GetAsync(user.AdministratedBy)
                        .ConfigureAwait(false) ?? throw new InvalidOperationException($"Actor for user {user.Id} not found.");

                    var organization = await _organizationRepository
                        .GetAsync(actor.OrganizationId)
                        .ConfigureAwait(false);

                    organizationNames[user.AdministratedBy]
                        = organizationName
                        = organization?.Name ?? throw new InvalidOperationException($"Organization for actor {actor.Id} not found.");
                }

                handledIdentities.Add(new AuditIdentityDto(userId.Value, organizationName));
            }
            else
            {
                nextIdentities.Add(user);
            }
        }

        var nextProcessing = await HandleUserNamesAsync(nextIdentities).ConfigureAwait(false);
        return handledIdentities.Concat(nextProcessing);
    }

    private async Task<IEnumerable<AuditIdentityDto>> HandleUserNamesAsync(IReadOnlyCollection<User> users)
    {
        var foundIdentities = new List<AuditIdentityDto>();

        var userIdentities = await _userIdentityRepository
            .GetUserIdentitiesAsync(users.Select(user => user.ExternalId))
            .ConfigureAwait(false);

        var lookup = userIdentities.ToDictionary(user => user.Id);

        foreach (var user in users)
        {
            var userIdentity = lookup.GetValueOrDefault(user.ExternalId);
            NotFoundValidationException.ThrowIfNull(userIdentity, user.ExternalId.Value, $"No external identity found for user id {user.Id}.");

            foundIdentities.Add(new AuditIdentityDto(user.Id.Value, $"{userIdentity.FirstName} ({userIdentity.Email})"));
        }

        return foundIdentities;
    }

    private bool HasCurrentUserAccessToActor(User user)
    {
        return _userContext.CurrentUser.IsFas ||
               _userContext.CurrentUser.IsAssignedToActor(user.AdministratedBy.Value) ||
               user.RoleAssignments
                   .Select(ura => ura.ActorId.Value)
                   .Any(_userContext.CurrentUser.IsAssignedToActor);
    }
}
