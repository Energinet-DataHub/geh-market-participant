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
using Energinet.DataHub.MarketParticipant.Application.Commands.Query.User;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Repositories.Query;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.User;

public sealed class GetUserOverviewHandler : IRequestHandler<GetUserOverviewCommand, GetUserOverviewResponse>
{
    private readonly IActorQueryRepository _actorQueryRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUserOverviewRepository _repository;
    private readonly IUserRoleRepository _userRoleRepository;

    public GetUserOverviewHandler(
        IActorQueryRepository actorQueryRepository,
        IOrganizationRepository organizationRepository,
        IUserRepository userRepository,
        IUserOverviewRepository repository,
        IUserRoleRepository userRoleRepository)
    {
        _actorQueryRepository = actorQueryRepository;
        _organizationRepository = organizationRepository;
        _userRepository = userRepository;
        _repository = repository;
        _userRoleRepository = userRoleRepository;
    }

    public async Task<GetUserOverviewResponse> Handle(GetUserOverviewCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var filter = request.Filter;

        IEnumerable<UserOverviewItem> users;
        int userCount;

        var sortProperty = (Domain.Model.Users.UserOverviewSortProperty)request.SortProperty;
        var sortDirection = (SortDirection)request.SortDirection;

        // The GetUsers function is kept, as it is more performant if no search criteria are used
        if (!string.IsNullOrEmpty(filter.SearchText) || filter.UserStatus.Any() || filter.UserRoleIds.Any())
        {
            var (items, totalCount) = await _repository
                .SearchUsersAsync(
                     request.PageNumber,
                     request.PageSize,
                     sortProperty,
                     sortDirection,
                     filter.ActorId,
                     filter.SearchText,
                     filter.UserStatus,
                     filter.UserRoleIds.Select(userRoleId => new UserRoleId(userRoleId)))
                .ConfigureAwait(false);

            users = items;
            userCount = totalCount;
        }
        else
        {
            users = await _repository.GetUsersAsync(
                request.PageNumber,
                request.PageSize,
                sortProperty,
                sortDirection,
                filter.ActorId).ConfigureAwait(false);

            userCount = await _repository
                .GetTotalUserCountAsync(filter.ActorId)
                .ConfigureAwait(false);
        }

        var mappedUsers = await PopulateUsersWithUserRolesAsync(users).ConfigureAwait(false);

        return new GetUserOverviewResponse(mappedUsers, userCount);
    }

    private async Task<IEnumerable<UserOverviewItemDto>> PopulateUsersWithUserRolesAsync(IEnumerable<UserOverviewItem> users)
    {
        var mappedUsers = new List<UserOverviewItemDto>();

        foreach (var user in users)
        {
            var userWithAssignments = await _userRepository
                .GetAsync(user.Id)
                .ConfigureAwait(false);

            var assignedUserRoles = new List<AssignedActorDto>();

            foreach (var assignmentsForActor in userWithAssignments!.RoleAssignments.GroupBy(assignment =>
                         assignment.ActorId))
            {
                var queryActor = await _actorQueryRepository
                    .GetActorAsync(assignmentsForActor.Key)
                    .ConfigureAwait(false);

                var organization = await _organizationRepository
                    .GetAsync(queryActor!.OrganizationId)
                    .ConfigureAwait(false);

                var actor = organization!.Actors.Single(a => a.Id == assignmentsForActor.Key);

                var userRoleNames = new List<string>();

                foreach (var userRoleId in assignmentsForActor.Select(x => x.UserRoleId))
                {
                    var userRole = await _userRoleRepository
                        .GetAsync(userRoleId)
                        .ConfigureAwait(false);

                    userRoleNames.Add(userRole!.Name);
                }

                assignedUserRoles.Add(new AssignedActorDto(
                    new ActorDto(
                        actor.ActorNumber.Value,
                        actor.Name.Value,
                        organization.Name),
                    userRoleNames));
            }

            mappedUsers.Add(new UserOverviewItemDto(
                user.Id.Value,
                user.Status,
                user.Name,
                user.Email.Address,
                user.PhoneNumber?.Number,
                user.CreatedDate,
                assignedUserRoles));
        }

        return mappedUsers;
    }
}
