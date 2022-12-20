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
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;

public sealed class UserOverviewRepository : IUserOverviewRepository
{
    private readonly IMarketParticipantDbContext _marketParticipantDbContext;
    private readonly IUserIdentityRepository _userIdentityRepository;

    public UserOverviewRepository(
        IMarketParticipantDbContext marketParticipantDbContext,
        IUserIdentityRepository userIdentityRepository)
    {
        _marketParticipantDbContext = marketParticipantDbContext;
        _userIdentityRepository = userIdentityRepository;
    }

    public Task<int> GetTotalUserCountAsync(Guid? actorId)
    {
        var query = BuildUsersQuery(actorId);
        return query.CountAsync();
    }

    public async Task<IEnumerable<UserOverviewItem>> GetUsersAsync(int pageNumber, int pageSize, Guid? actorId)
    {
        var query = BuildUsersQuery(actorId);

        var users = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new { x.Id, x.ExternalId, x.Email })
            .ToListAsync()
            .ConfigureAwait(false);

        var userLookup = users.ToDictionary(
            x => new ExternalUserId(x.ExternalId),
            y => new
            {
                Id = new UserId(y.Id),
                ExternalId = new ExternalUserId(y.ExternalId),
                Email = new EmailAddress(y.Email)
            });

        var userIdentities = await _userIdentityRepository
            .GetUserIdentitiesAsync(userLookup.Keys)
            .ConfigureAwait(false);

        return userIdentities.Select(userIdentity =>
        {
            var user = userLookup[userIdentity.Id];
            return new UserOverviewItem(
                user.Id,
                userIdentity.Email ?? user.Email,
                userIdentity.Name,
                userIdentity.PhoneNumber,
                userIdentity.CreatedDate,
                userIdentity.Enabled);
        });
    }

    private IQueryable<UserEntity> BuildUsersQuery(Guid? actorId)
    {
        var query =
            from user in _marketParticipantDbContext.Users
            select user;

        if (actorId != null)
        {
            query = from u in query
                    join a in _marketParticipantDbContext.UserRoleAssignments
                        on new { UserId = u.Id, ActorId = actorId.Value } equals new { a.UserId, a.ActorId }
                    select u;
        }

        return query.OrderBy(x => x.Email).Distinct();
    }
}
