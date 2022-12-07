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
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories.Slim;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;

public sealed class UserOverviewRepository : IUserOverviewRepository
{
    private readonly IMarketParticipantDbContext _marketParticipantDbContext;

    public UserOverviewRepository(IMarketParticipantDbContext marketParticipantDbContext)
    {
        _marketParticipantDbContext = marketParticipantDbContext;
    }

    public Task<int> GetUsersPageCountAsync(int pageSize, Guid? actorId)
    {
        var query = BuildUsersQuery(actorId);
        return query.CountAsync();
    }

    public async Task<IEnumerable<UserOverviewItem>> GetUsersAsync(int pageNumber, int pageSize, Guid? actorId)
    {
        var query = BuildUsersQuery(actorId);

        query = query.Skip((pageNumber - 1) * pageSize).Take(pageSize);

        return (await query.Select(x => new { x.Id, x.Email }).ToListAsync().ConfigureAwait(false))
            .Select(x => new UserOverviewItem(
                new UserId(x.Id),
                new Domain.Model.EmailAddress(x.Email)));
    }

    private IQueryable<UserEntity> BuildUsersQuery(Guid? actorId)
    {
        var query = from user in _marketParticipantDbContext.Users
                    select user;

        if (actorId != null)
        {
            query = from user in query
                    join assi in _marketParticipantDbContext.UserRoleAssignments
                        on new { UserId = user.Id, ActorId = actorId.Value } equals new { UserId = assi.UserId, ActorId = assi.ActorId }
                    select user;
        }

        return query.OrderBy(x => x.Email).Distinct();
    }
}
