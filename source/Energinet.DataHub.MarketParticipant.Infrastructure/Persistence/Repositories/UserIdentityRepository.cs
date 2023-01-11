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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Microsoft.Graph;
using EmailAddress = Energinet.DataHub.MarketParticipant.Domain.Model.EmailAddress;
using User = Microsoft.Graph.User;
using UserIdentity = Energinet.DataHub.MarketParticipant.Domain.Model.UserIdentity;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;

public sealed class UserIdentityRepository : IUserIdentityRepository
{
    private readonly GraphServiceClient _graphClient;

    public UserIdentityRepository(GraphServiceClient graphClient)
    {
        _graphClient = graphClient;
    }

    public async Task<IEnumerable<UserIdentity>> SearchUserIdentitiesAsync(string? searchText, bool? active)
    {
        var queryOptions = new List<Option>
        {
            new HeaderOption("ConsistencyLevel", "eventual"),
            new QueryOption("$count", "true"),
        };

        var request = _graphClient.Users.Request(queryOptions);

        if (!string.IsNullOrEmpty(searchText))
        {
            request = request.Filter($"startswith(displayName, '{searchText}')");
        }

        if (active.HasValue)
        {
            request = request.Filter($"accountEnabled eq '{active.Value}'");
        }

        var users = await request
            .Select(x => new { x.Id, x.DisplayName, x.Identities, x.MobilePhone, x.CreatedDateTime, x.AccountEnabled })
            .GetAsync()
            .ConfigureAwait(false);

        return await IterateUsersAsync(users).ConfigureAwait(false);
    }

    public async Task<UserIdentity> GetUserIdentityAsync(ExternalUserId externalId)
    {
        ArgumentNullException.ThrowIfNull(externalId);

        var user = await _graphClient
            .Users[externalId.Value.ToString()]
            .Request()
            .Select(x => new { x.Id, x.DisplayName, x.Identities, x.MobilePhone, x.CreatedDateTime, x.AccountEnabled })
            .GetAsync()
            .ConfigureAwait(false);

        return Map(user);
    }

    public async Task<IEnumerable<UserIdentity>> GetUserIdentitiesAsync(IEnumerable<ExternalUserId> externalIds)
    {
        var ids = externalIds.Distinct();
        var result = new List<UserIdentity>();

        foreach (var segment in ids.Chunk(15))
        {
            var users = await _graphClient.Users
                .Request()
                .Filter($"id in ({string.Join(",", segment.Select(x => $"'{x}'"))})")
                .Select(x => new { x.Id, x.DisplayName, x.Identities, x.MobilePhone, x.CreatedDateTime, x.AccountEnabled })
                .GetAsync()
                .ConfigureAwait(false);

            result.AddRange(await IterateUsersAsync(users).ConfigureAwait(false));
        }

        return result;
    }

    private static UserIdentity Map(User user)
    {
        var userEmailAddress = user
            .Identities
            .Where(ident => ident.SignInType == "emailAddress")
            .Select(ident => ident.IssuerAssignedId)
            .First();

        return new UserIdentity(
            new ExternalUserId(user.Id),
            user.DisplayName,
            new EmailAddress(userEmailAddress),
            string.IsNullOrWhiteSpace(user.MobilePhone) ? null : new PhoneNumber(user.MobilePhone),
            user.CreatedDateTime!.Value,
            user.AccountEnabled == true);
    }

    private async Task<IEnumerable<UserIdentity>> IterateUsersAsync(IGraphServiceUsersCollectionPage users)
    {
        var results = new List<UserIdentity>();
        var pageIterator = PageIterator<User>
            .CreatePageIterator(
                _graphClient,
                users,
                user =>
                {
                    var userEmailAddress = user
                        .Identities
                        .Where(ident => ident.SignInType == "emailAddress")
                        .Select(ident => ident.IssuerAssignedId)
                        .FirstOrDefault();

                    // Because of missing invite flow, we do not currently know whether
                    // the found user can sign-in or is created for other purposes (like administration of B2C).
                    // For now, we skip everything that is not emailAddress.
                    if (userEmailAddress != null)
                    {
                        results.Add(Map(user));
                    }

                    return true;
                });

        while (pageIterator.State != PagingState.Complete)
        {
            await pageIterator.IterateAsync().ConfigureAwait(false);
        }

        return results;
    }
}
