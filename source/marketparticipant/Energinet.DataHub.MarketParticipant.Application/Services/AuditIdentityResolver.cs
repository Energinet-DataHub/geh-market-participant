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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users.Authentication;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;

namespace Energinet.DataHub.MarketParticipant.Application.Services;

public sealed class AuditIdentityResolver : IAuditIdentityResolver
{
    private static readonly IReadOnlyDictionary<AuditIdentity, KnownAuditIdentityProvider> _knownAuditIdentities
        = new Dictionary<AuditIdentity, KnownAuditIdentityProvider>
    {
        { KnownAuditIdentityProvider.Migration.IdentityId, KnownAuditIdentityProvider.Migration },
        { KnownAuditIdentityProvider.TestFramework.IdentityId, KnownAuditIdentityProvider.TestFramework },
        { KnownAuditIdentityProvider.OrganizationBackgroundService.IdentityId, KnownAuditIdentityProvider.OrganizationBackgroundService },
    };

    private readonly IUserRepository _userRepository;
    private readonly IUserIdentityRepository _userIdentityRepository;

    public AuditIdentityResolver(
        IUserRepository userRepository,
        IUserIdentityRepository userIdentityRepository)
    {
        _userRepository = userRepository;
        _userIdentityRepository = userIdentityRepository;
    }

    public async Task<UserIdentity> ResolveAsync(AuditIdentity identityId)
    {
        ArgumentNullException.ThrowIfNull(identityId);

        if (_knownAuditIdentities.TryGetValue(identityId, out var knownAuditIdentity))
        {
            return new UserIdentity(
                new ExternalUserId(Guid.Empty),
                new EmailAddress("noreply@datahub.dk"),
                UserIdentityStatus.Active,
                "DataHub",
                knownAuditIdentity.FriendlyName,
                null,
                new DateTime(2023, 1, 1),
                AuthenticationMethod.Undetermined,
                Array.Empty<LoginIdentity>());
        }

        var user = await _userRepository
            .GetAsync(new UserId(identityId.Value))
            .ConfigureAwait(false);

        if (user == null)
            throw new InvalidOperationException($"Audited user with id '{identityId}' not found.");

        var userIdentity = await _userIdentityRepository
            .GetAsync(user.ExternalId)
            .ConfigureAwait(false);

        if (userIdentity == null)
            throw new InvalidOperationException($"Audited user with id '{identityId}' not found.");

        return userIdentity;
    }
}
