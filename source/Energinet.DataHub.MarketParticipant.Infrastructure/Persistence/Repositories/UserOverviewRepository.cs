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
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Repositories.Query;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Microsoft.EntityFrameworkCore;
using EmailAddress = Energinet.DataHub.MarketParticipant.Domain.Model.EmailAddress;

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

    public Task<int> GetUsersPageCountAsync(int pageSize, Guid? actorId)
    {
        var query = BuildUsersQuery(actorId, null, string.Empty);
        return query.CountAsync();
    }

    public async Task<IEnumerable<UserOverviewItem>> GetUsersAsync(int pageNumber, int pageSize, Guid? actorId)
    {
        var query = BuildUsersQuery(actorId, null, null);
        var userLookup = (await query.Select(x => new { x.Id, x.ExternalId, x.Email }).ToListAsync().ConfigureAwait(false))
            .ToDictionary(x => x.ExternalId);

        var userIdentities = await _userIdentityRepository.GetUserIdentitiesAsync(userLookup.Keys).ConfigureAwait(false);

        return userIdentities.Where(x => userLookup.ContainsKey(x.Id)).Skip((pageNumber - 1) * pageSize).Take(pageSize).Select(userIdentity =>
        {
            var user = userLookup[userIdentity.Id];
            return new UserOverviewItem(
                new UserId(user.Id),
                new EmailAddress(userIdentity.Email ?? user.Email),
                userIdentity.Name,
                userIdentity.PhoneNumber,
                userIdentity.CreatedDate,
                userIdentity.Enabled);
        });
    }

    public async Task<IEnumerable<UserOverviewItem>> SearchUsersAsync(
        int pageNumber,
        int pageSize,
        Guid? actorId,
        string? searchText,
        bool? onlyActive,
        Collection<EicFunction>? eicFunctions)
    {
        var query = BuildUsersQuery(actorId, eicFunctions, searchText);
        var userLookup = (await query.Select(x => new { x.Id, x.ExternalId, x.Email }).ToListAsync().ConfigureAwait(false))
            .ToDictionary(x => x.ExternalId);
        var userIdentities = await _userIdentityRepository.SearchUserIdentitiesAsync(searchText, onlyActive).ConfigureAwait(false);

        // Filter User Identities to only be from our user pool
        return userIdentities.Where(x => userLookup.ContainsKey(x.Id)).Skip((pageNumber - 1) * pageSize).Take(pageSize).Select(userIdentity =>
        {
            var user = userLookup[userIdentity.Id];
            return new UserOverviewItem(
                new UserId(user.Id),
                new EmailAddress(userIdentity.Email ?? user.Email),
                userIdentity.Name,
                userIdentity.PhoneNumber,
                userIdentity.CreatedDate,
                userIdentity.Enabled);
        });
    }

    private IQueryable<UserEntity> BuildUsersQuery(Guid? actorId, Collection<EicFunction>? eicFunctions, string? searchText)
    {
        var query = from u in _marketParticipantDbContext.Users
            join r in _marketParticipantDbContext.UserRoleAssignments on u.Id equals r.UserId into urj
            from urr in urj.DefaultIfEmpty()
            join ur in _marketParticipantDbContext.UserRoles on urr.UserRoleId equals ur.Id into urt
            from urtj in urt.DefaultIfEmpty()
            join actor in _marketParticipantDbContext.Actors on urr.ActorId equals actor.Id
            where
                (actorId == null || urr.ActorId == actorId)
                && (eicFunctions == null || !eicFunctions.Any() || urtj.EicFunctions.All(q => eicFunctions.Contains(q.EicFunction)))
                && (searchText == null || actor.Name.Contains(searchText) || actor.ActorNumber.Contains(searchText) || urtj.Name.Contains(searchText))
            select u;

        return query.OrderBy(x => x.Email).Distinct();
    }
}
