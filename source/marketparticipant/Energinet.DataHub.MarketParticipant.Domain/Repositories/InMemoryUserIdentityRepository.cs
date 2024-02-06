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
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users.Authentication;

namespace Energinet.DataHub.MarketParticipant.Domain.Repositories;

public sealed class InMemoryUserIdentityRepository : IUserIdentityRepository
{
    private static readonly List<UserIdentity> _identities = new()
    {
        new UserIdentity(
            new ExternalUserId(Guid.Parse("00000000-1100-1111-0011-000000000000")),
            new EmailAddress("bootstrap.datahub@energinet.dk"),
            UserIdentityStatus.Active,
            "Bootstrap",
            "Datahub",
            null,
            DateTimeOffset.Parse("2024-02-06T00:00:00Z", CultureInfo.InvariantCulture),
            AuthenticationMethod.Undetermined,
            Enumerable.Empty<LoginIdentity>()),
    };

    public Task<UserIdentity?> GetAsync(ExternalUserId externalId)
    {
        return Task.FromResult(_identities.FirstOrDefault(x => x.Id == externalId));
    }

    public Task<UserIdentity?> FindIdentityReadyForOpenIdSetupAsync(ExternalUserId externalId)
    {
        return Task.FromResult<UserIdentity?>(null);
    }

    public Task<UserIdentity?> GetAsync(EmailAddress email)
    {
        return Task.FromResult(_identities.FirstOrDefault(x => x.Email == email));
    }

    public Task<IEnumerable<UserIdentity>> GetUserIdentitiesAsync(IEnumerable<ExternalUserId> externalIds)
    {
        return Task.FromResult(_identities.Where(x => externalIds.Contains(x.Id)));
    }

    public Task<IEnumerable<UserIdentity>> SearchUserIdentitiesAsync(string? searchText, bool? accountEnabled)
    {
        var result = _identities;
        if (searchText != null)
        {
            result = result.Where(x =>
                    x.FirstName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    x.LastName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    x.Email.Address.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        result = accountEnabled switch
        {
            true => result.Where(x => x.Status == UserIdentityStatus.Active).ToList(),
            false => result.Where(x => x.Status == UserIdentityStatus.Inactive).ToList(),
            _ => result,
        };

        return Task.FromResult<IEnumerable<UserIdentity>>(result);
    }

    public Task<ExternalUserId> CreateAsync(UserIdentity userIdentity)
    {
        ArgumentNullException.ThrowIfNull(userIdentity);

        var newIdentity = new UserIdentity(
            new ExternalUserId(Guid.NewGuid()),
            userIdentity.Email,
            userIdentity.Status,
            userIdentity.FirstName,
            userIdentity.LastName,
            userIdentity.PhoneNumber,
            userIdentity.CreatedDate,
            userIdentity.Authentication,
            userIdentity.LoginIdentities);

        _identities.Add(newIdentity);

        return Task.FromResult(newIdentity.Id);
    }

    public Task UpdateUserAsync(UserIdentity userIdentity)
    {
        _identities.RemoveAll(x => x.Id == userIdentity.Id);
        _identities.Add(userIdentity);
        return Task.CompletedTask;
    }

    public Task AssignUserLoginIdentitiesAsync(UserIdentity userIdentity)
    {
        return Task.CompletedTask;
    }

    public Task DeleteAsync(ExternalUserId externalUserId)
    {
        _identities.RemoveAll(x => x.Id == externalUserId);
        return Task.CompletedTask;
    }

    public Task DisableUserAccountAsync(ExternalUserId externalUserId)
    {
        var userIdentity = _identities.First(x => x.Id == externalUserId);

        var newIdentity = new UserIdentity(
            userIdentity.Id,
            userIdentity.Email,
            UserIdentityStatus.Inactive,
            userIdentity.FirstName,
            userIdentity.LastName,
            userIdentity.PhoneNumber,
            userIdentity.CreatedDate,
            userIdentity.Authentication,
            userIdentity.LoginIdentities);

        _identities.Remove(userIdentity);
        _identities.Add(newIdentity);
        return Task.CompletedTask;
    }

    public Task EnableUserAccountAsync(ExternalUserId externalUserId)
    {
        var userIdentity = _identities.First(x => x.Id == externalUserId);

        var newIdentity = new UserIdentity(
            userIdentity.Id,
            userIdentity.Email,
            UserIdentityStatus.Active,
            userIdentity.FirstName,
            userIdentity.LastName,
            userIdentity.PhoneNumber,
            userIdentity.CreatedDate,
            userIdentity.Authentication,
            userIdentity.LoginIdentities);

        _identities.Remove(userIdentity);
        _identities.Add(newIdentity);
        return Task.CompletedTask;
    }
}
