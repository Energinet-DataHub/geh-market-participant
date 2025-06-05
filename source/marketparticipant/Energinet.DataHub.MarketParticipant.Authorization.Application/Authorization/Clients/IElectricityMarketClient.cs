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

using Energinet.DataHub.MarketParticipant.Authorization.Model;
using NodaTime;

namespace Energinet.DataHub.MarketParticipant.Authorization.Application.Authorization.Clients;

public interface IElectricityMarketClient
{
    /// <summary>
    /// Verify that get master data is allowed for the current user.
    /// </summary>
    /// <param name="meteringPointId">The identifier of the metering point.</param>
    /// <param name="gridAreaCodes">List of grid Areas that are valid for the grid access provider as of now.</param>
    /// <returns>The list of metering point master data changes within the specified period.</returns>
    Task<bool> VerifyMeteringPointIsInGridAreaAsync(string meteringPointId, IEnumerable<string> gridAreaCodes);

    /// <summary>
    /// Get the list of periods where the balance suoplier has a commercial relation on the metering point with the requested period.
    /// </summary>
    /// <param name="meteringPointId">The identifier of the metering point.</param>
    /// <param name="actorNumber">The id of the balance supplier.</param>
    /// <param name="requestedPeriod">The period where the results should fit.</param>
    /// <returns>List of periods where the balance supllier has a commercial relation with the metering point.</returns>
    Task<IEnumerable<AccessPeriod>> GetSupplierPeriodsAsync(string meteringPointId, string actorNumber, Interval requestedPeriod);
}
