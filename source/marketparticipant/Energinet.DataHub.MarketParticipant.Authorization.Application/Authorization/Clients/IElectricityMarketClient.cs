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

using System.Collections.ObjectModel;

namespace Energinet.DataHub.MarketParticipant.Authorization.Application.Authorization.Clients;

public interface IElectricityMarketClient
{
    /// <summary>
    /// Verify that get master data is allowed for the current user.
    /// </summary>
    /// <param name="meteringPointId">The identifier of the metering point.</param>
    /// <param name="gridAreaCode">List of grid Areas that are valid for the grid access provider as of now.</param>
    /// <returns>The list of metering point master data changes within the specified period.</returns>
    Task<bool> GetMeteringPointMasterDataForGridAccessProviderAllowedAsync(string meteringPointId, ReadOnlyCollection<string> gridAreaCode);
}
