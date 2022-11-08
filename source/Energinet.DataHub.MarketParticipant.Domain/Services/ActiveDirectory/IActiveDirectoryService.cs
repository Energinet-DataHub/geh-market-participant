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
using System.Net.Mime;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;

namespace Energinet.DataHub.MarketParticipant.Domain.Services.ActiveDirectory;

/// <summary>
/// This service provides access to Active directory for managing Actor Application registrations
/// </summary>
public interface IActiveDirectoryService
{
    /// <summary>
    /// Lists all Actor application registrations in AD
    /// </summary>
    /// <returns>List of all actor application registrations with id and display name</returns>
    Task<IEnumerable<(string AppId, string DisplayName)>> ListActorsAsync();

    /// <summary>
    /// Creates an Actor application registration in Azure AD
    /// </summary>
    /// <param name="actor">The actor to create or update the App Registration for</param>
    /// <returns>The app id of the created or updated Application registration to this Actor</returns>
    Task<string> CreateOrUpdateAppAsync(Actor actor);

    /// <summary>
    /// Deletes an actor from AD
    /// </summary>
    /// <param name="actor"></param>
    /// <returns>Nothing</returns>
    Task DeleteActorAsync(Actor actor);

    /// <summary>
    /// Check if application registration exists in AD
    /// </summary>
    /// <returns>true if found, false if not</returns>
    Task<bool> AppExistsAsync(Actor actor);
}
