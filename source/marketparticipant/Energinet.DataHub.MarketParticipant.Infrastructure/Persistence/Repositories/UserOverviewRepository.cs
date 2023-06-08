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
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Energinet.DataHub.MarketParticipant.Infrastructure.Extensions;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;

public sealed class UserOverviewRepository : IUserOverviewRepository
{
    private readonly IMarketParticipantDbContext _marketParticipantDbContext;
    private readonly IUserIdentityRepository _userIdentityRepository;
    private readonly IUserStatusCalculator _userStatusCalculator;

    public UserOverviewRepository(
        IMarketParticipantDbContext marketParticipantDbContext,
        IUserIdentityRepository userIdentityRepository,
        IUserStatusCalculator userStatusCalculator)
    {
        _marketParticipantDbContext = marketParticipantDbContext;
        _userIdentityRepository = userIdentityRepository;
        _userStatusCalculator = userStatusCalculator;
    }

    public Task<int> GetTotalUserCountAsync(ActorId? actorId)
    {
        var query = BuildUsersSearchQuery(actorId, null, Enumerable.Empty<UserRoleId>());
        return query.CountAsync();
    }

    public async Task<IEnumerable<UserOverviewItem>> GetUsersAsync(
        int pageNumber,
        int pageSize,
        UserOverviewSortProperty sortProperty,
        SortDirection sortDirection,
        ActorId? actorId)
    {
        var query = BuildUsersSearchQuery(actorId, null, Enumerable.Empty<UserRoleId>());
        var users = await query
            .Select(x => new { x.Id, x.ExternalId, x.InvitationExpiresAt })
            .ToListAsync()
            .ConfigureAwait(false);

        var userLookup = users.ToDictionary(
            x => new ExternalUserId(x.ExternalId),
            y => new
            {
                Id = new UserId(y.Id),
                ExternalId = new ExternalUserId(y.ExternalId),
                y.InvitationExpiresAt
            });

        var userIdentities = (await _userIdentityRepository
            .GetUserIdentitiesAsync(userLookup.Keys)
            .ConfigureAwait(false))
            .Select(userIdentity =>
                {
                    var user = userLookup[userIdentity.Id];
                    return new
                    {
                        user.Id,
                        userIdentity.Status,
                        userIdentity.FirstName,
                        userIdentity.LastName,
                        Email = userIdentity.Email.Address,
                        PhoneNumber = userIdentity.PhoneNumber?.Number,
                        userIdentity.CreatedDate,
                        user.InvitationExpiresAt
                    };
                });

        userIdentities =
            sortDirection == SortDirection.Asc
                ? userIdentities.OrderBy(sortProperty.ToString())
                : userIdentities.OrderByDescending(sortProperty.ToString());

        return userIdentities
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x =>
                new UserOverviewItem(
                    x.Id,
                    _userStatusCalculator.CalculateUserStatus(x.Status, x.InvitationExpiresAt),
                    x.FirstName,
                    x.LastName,
                    new EmailAddress(x.Email),
                    x.PhoneNumber != null ? new PhoneNumber(x.PhoneNumber) : null,
                    x.CreatedDate));
    }

    public async Task<(IEnumerable<UserOverviewItem> Items, int TotalCount)> SearchUsersAsync(
        int pageNumber,
        int pageSize,
        UserOverviewSortProperty sortProperty,
        SortDirection sortDirection,
        ActorId? actorId,
        string? searchText,
        IEnumerable<UserStatus> userStatus,
        IEnumerable<UserRoleId> userRoles)
    {
        var userRolesFilter = userRoles.ToList();
        var statusFilter = userStatus.ToHashSet();
        var userIdentityFilter = statusFilter.Select(status => status switch
        {
            UserStatus.Inactive => false,
            UserStatus.InviteExpired => false,
            UserStatus.Active => true,
            UserStatus.Invited => true,
            _ => (bool?)null
        }).ToHashSet();

        var accountEnabledFilter = userIdentityFilter.Count is 0 or 2
            ? null
            : userIdentityFilter.Single();

        // We need to do two searches and two lookup, since the queries in either our data or AD can return results not in the other, and we need AD data for both
        // Search and then Filter only users from the AD search that have an ID in our local data
        var searchUserIdentities = (await _userIdentityRepository
            .SearchUserIdentitiesAsync(searchText, accountEnabledFilter)
            .ConfigureAwait(false))
            .ToList();

        var knownLocalUsers = await BuildUserLookupQuery(
                actorId,
                searchUserIdentities.Select(x => x.Id),
                userRolesFilter)
            .Select(y => new { y.Id, y.ExternalId, y.InvitationExpiresAt })
            .ToListAsync()
            .ConfigureAwait(false);

        var knownLocalIds = knownLocalUsers.Select(x => x.ExternalId);
        searchUserIdentities = searchUserIdentities
            .Where(x => knownLocalIds.Contains(x.Id.Value))
            .ToList();

        // Search local data and then fetch data from AD for results from our own data, that wasn't in the already found identities
        var searchQuery = await BuildUsersSearchQuery(actorId, searchText, userRolesFilter)
            .Select(x => new { x.Id, x.ExternalId, x.InvitationExpiresAt })
            .ToListAsync()
            .ConfigureAwait(false);

        var localUserIdentitiesLookup = await _userIdentityRepository
            .GetUserIdentitiesAsync(searchQuery
                .Select(x => x.ExternalId)
                .Except(knownLocalIds)
                .Select(x => new ExternalUserId(x)))
            .ConfigureAwait(false);

        // Combine results and create final search result
        var userLookup = searchQuery
            .Union(knownLocalUsers)
            .ToDictionary(x => x.ExternalId);

        var allIdentities = searchUserIdentities
            .Union(localUserIdentitiesLookup)
            .Where(x =>
            {
                var user = userLookup[x.Id.Value];
                var status = _userStatusCalculator.CalculateUserStatus(x.Status, user.InvitationExpiresAt);
                return !statusFilter.Any() || statusFilter.Contains(status);
            })
            .Select(x => new
            {
                x.Id,
                x.Status,
                x.FirstName,
                x.LastName,
                Email = x.Email.Address,
                PhoneNumber = x.PhoneNumber?.Number,
                x.CreatedDate
            });

        allIdentities =
            sortDirection == SortDirection.Asc
                ? allIdentities.OrderBy(sortProperty.ToString())
                : allIdentities.OrderByDescending(sortProperty.ToString());

        var allIdentitiesEnumerated = allIdentities.ToList();

        // Filter User Identities to only be from our user pool
        var items = allIdentitiesEnumerated
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(userIdentity =>
            {
                var user = userLookup[userIdentity.Id.Value];
                return new UserOverviewItem(
                    new UserId(user.Id),
                    _userStatusCalculator.CalculateUserStatus(userIdentity.Status, user.InvitationExpiresAt),
                    userIdentity.FirstName,
                    userIdentity.LastName,
                    new EmailAddress(userIdentity.Email),
                    userIdentity.PhoneNumber != null ? new PhoneNumber(userIdentity.PhoneNumber) : null,
                    userIdentity.CreatedDate);
            });

        return (items, allIdentitiesEnumerated.Count);
    }

    private IQueryable<UserEntity> BuildUsersSearchQuery(
        ActorId? actorId,
        string? searchText,
        IEnumerable<UserRoleId> userRoleIds)
    {
        var userRoles = userRoleIds.Select(x => x.Value).ToList();

        var query =
            from u in _marketParticipantDbContext.Users
            join r in _marketParticipantDbContext.UserRoleAssignments on u.Id equals r.UserId
            where !userRoles.Any() || userRoles.Contains(r.UserRoleId)
            join ur in _marketParticipantDbContext.UserRoles on r.UserRoleId equals ur.Id
            join actor in _marketParticipantDbContext.Actors on r.ActorId equals actor.Id
            where
                (actorId == null || r.ActorId == actorId.Value)
                && (searchText == null || actor.Name.Contains(searchText) || actor.ActorNumber.Contains(searchText) || ur.Name.Contains(searchText))
            select u;

        return query.Distinct();
    }

    private IQueryable<UserEntity> BuildUserLookupQuery(
        ActorId? actorId,
        IEnumerable<ExternalUserId> externalUserIds,
        IEnumerable<UserRoleId> userRoleIds)
    {
        var userRoles = userRoleIds.Select(x => x.Value).ToList();
        var externalUsers = externalUserIds.Select(x => x.Value);

        var query =
            from u in _marketParticipantDbContext.Users
            join r in _marketParticipantDbContext.UserRoleAssignments on u.Id equals r.UserId
            where !userRoles.Any() || userRoles.Contains(r.UserRoleId)
            where
                (actorId == null || r.ActorId == actorId.Value)
                && externalUsers.Contains(u.ExternalId)
            select u;

        return query.Distinct();
    }
}
