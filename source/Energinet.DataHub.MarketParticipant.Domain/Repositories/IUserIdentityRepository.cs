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
using Energinet.DataHub.MarketParticipant.Domain.Model;

namespace Energinet.DataHub.MarketParticipant.Domain.Repositories;

/// <summary>
/// Gives access to user identity information
/// </summary>
public interface IUserIdentityRepository
{
    /// <summary>
    /// Searches for users using a search text
    /// </summary>
    /// <param name="searchText">The text to search for</param>
    /// <returns>A List of users matching the search text</returns>
    /// <remarks>Currently searches Name, Phone and Email</remarks>
    Task<IEnumerable<UserIdentity>> SearchUserIdentitiesAsync(string? searchText);

    /// <summary>
    /// Retrieves user identities for the provided set of external ids.
    /// </summary>
    /// <param name="externalIds"></param>
    Task<IEnumerable<UserIdentity>> GetUserIdentitiesAsync(IEnumerable<Guid> externalIds);
}
