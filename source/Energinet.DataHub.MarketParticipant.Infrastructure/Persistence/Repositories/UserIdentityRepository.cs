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

    public async Task<IEnumerable<UserIdentity>> SearchUserIdentitiesAsync(string? searchText)
    {
        var result = new List<UserIdentity>();
        var queryOptions = new List<Option>()
        {
            new HeaderOption("ConsistencyLevel", "eventual"),
            new QueryOption("$count", "true"),
        };

        var users = await _graphClient.Users
            .Request(queryOptions)
            .Select(x => new { x.Id, x.DisplayName, x.Mail, x.MobilePhone, x.CreatedDateTime, x.AccountEnabled })
            .GetAsync()
            .ConfigureAwait(false);

        if (!string.IsNullOrEmpty(searchText))
        {
            // TODO: Add MobilePhone once we are switched to Azure AD, since currently we are running on Azure B2C where it is not supported.
            users = await _graphClient.Users
                .Request(queryOptions)
                .Filter($"startswith(displayName, '{searchText}')")
                .Select(x => new { x.Id, x.DisplayName, x.Mail, x.MobilePhone, x.CreatedDateTime, x.AccountEnabled })
                .GetAsync()
                .ConfigureAwait(false);
        }

        var pageIterator = PageIterator<User>
            .CreatePageIterator(
                _graphClient,
                users,
                (user) =>
                {
                    result.Add(new UserIdentity(
                        new ExternalUserId(user.Id),
                        user.DisplayName,
                        string.IsNullOrWhiteSpace(user.Mail) ? null : new EmailAddress(user.Mail),
                        string.IsNullOrWhiteSpace(user.MobilePhone) ? null : new PhoneNumber(user.MobilePhone),
                        user.CreatedDateTime!.Value,
                        user.AccountEnabled == true));

                    return true;
                });

        while (pageIterator.State != PagingState.Complete)
        {
            await pageIterator.IterateAsync().ConfigureAwait(false);
        }

        return result;
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
                .Select(x => new { x.Id, x.DisplayName, x.Mail, x.MobilePhone, x.CreatedDateTime, x.AccountEnabled })
                .GetAsync()
                .ConfigureAwait(false);

            var pageIterator = PageIterator<User>
                .CreatePageIterator(
                    _graphClient,
                    users,
                    user =>
                    {
                        result.Add(new Domain.Model.UserIdentity(
                            new ExternalUserId(user.Id),
                            user.DisplayName,
                            string.IsNullOrWhiteSpace(user.Mail) ? null : new EmailAddress(user.Mail),
                            string.IsNullOrWhiteSpace(user.MobilePhone) ? null : new PhoneNumber(user.MobilePhone),
                            user.CreatedDateTime!.Value,
                            user.AccountEnabled == true));

                        return true;
                    });

            while (pageIterator.State != PagingState.Complete)
            {
                await pageIterator.IterateAsync().ConfigureAwait(false);
            }
        }

        return result;
    }
}
