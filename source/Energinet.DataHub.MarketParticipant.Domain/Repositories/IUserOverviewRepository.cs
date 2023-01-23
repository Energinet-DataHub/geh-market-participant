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
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;

namespace Energinet.DataHub.MarketParticipant.Domain.Repositories;

/// <summary>
/// Repository for presenting an overview of users paged.
/// </summary>
public interface IUserOverviewRepository
{
    /// <summary>
    /// Calculates total number of users.
    /// </summary>
    /// <param name="actorId">The id of the actor.</param>
    /// <returns>The number of users.</returns>
    Task<int> GetTotalUserCountAsync(Guid? actorId);

    /// <summary>
    /// Retrieve users paged.
    /// </summary>
    /// <param name="pageNumber">Page number.</param>
    /// <param name="pageSize">Page size.</param>
    /// <param name="actorId">The id of the actor.</param>
    /// <returns>A list of users.</returns>
    Task<IEnumerable<UserOverviewItem>> GetUsersAsync(int pageNumber, int pageSize, Guid? actorId);

    /// <summary>
    /// Searches users paged
    /// </summary>
    /// <param name="pageNumber">Page number.</param>
    /// <param name="pageSize">Page size.</param>
    /// <param name="actorId">The id of the actor.</param>
    /// <param name="searchText">A text to search for, can be empty</param>
    /// <param name="userStatus">Specifies which user status the search should filter on.</param>
    /// <returns>A List of users matching the criteria supplied</returns>
    Task<(IEnumerable<UserOverviewItem> Items, int TotalCount)> SearchUsersAsync(
        int pageNumber,
        int pageSize,
        Guid? actorId,
        string? searchText,
        IEnumerable<UserStatus> userStatus);
}
