﻿// Copyright 2020 Energinet DataHub A/S
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
using Energinet.DataHub.MarketParticipant.Application.Commands.Query.Users;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Users;

public sealed class GetUserOverviewHandler : IRequestHandler<GetUserOverviewCommand, GetUserOverviewResponse>
{
    private readonly IUserOverviewRepository _repository;

    public GetUserOverviewHandler(IUserOverviewRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetUserOverviewResponse> Handle(GetUserOverviewCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var filter = request.Filter;

        IEnumerable<UserOverviewItem> users;
        int userCount;

        var sortProperty = (Domain.Model.Users.UserOverviewSortProperty)request.SortProperty;
        var sortDirection = (SortDirection)request.SortDirection;
        var actorId = filter.ActorId.HasValue
            ? new ActorId(filter.ActorId.Value)
            : null;

        // The GetUsers function is kept, as it is more performant if no search criteria are used
        if (!string.IsNullOrEmpty(filter.SearchText) || filter.UserStatus.Any() || filter.UserRoleIds.Any())
        {
            var (items, totalCount) = await _repository
                .SearchUsersAsync(
                     request.PageNumber,
                     request.PageSize,
                     sortProperty,
                     sortDirection,
                     actorId,
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
                actorId).ConfigureAwait(false);

            userCount = await _repository
                .GetTotalUserCountAsync(actorId)
                .ConfigureAwait(false);
        }

        var mappedUsers = users
            .Select(user => new UserOverviewItemDto(
                user.Id.Value,
                user.Status,
                user.FirstName,
                user.LastName,
                user.Email.Address,
                user.PhoneNumber?.Number,
                user.CreatedDate,
                user.AdministratedBy,
                user.LatestLoginAt))
            .ToList();

        return new GetUserOverviewResponse(mappedUsers, userCount);
    }
}
