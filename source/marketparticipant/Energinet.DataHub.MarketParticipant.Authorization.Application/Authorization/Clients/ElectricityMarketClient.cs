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

using System.Net;
using System.Net.Http.Json;

namespace Energinet.DataHub.MarketParticipant.Authorization.Application.Authorization.Clients;

public sealed class ElectricityMarketClient : IElectricityMarketClient
{
    private readonly HttpClient _apiHttpClient;

    internal ElectricityMarketClient(HttpClient apiHttpClient)
    {
        _apiHttpClient = apiHttpClient;
    }

    public async Task<bool> GetMeteringPointMasterDataForGridAccessProviderAllowedAsync(string meteringPointId, List<string> gridAreas)
    {
        ArgumentNullException.ThrowIfNull(meteringPointId);

        // TODO: Make new API in electricity market
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/metering-point/verify-grid-owner");
        request.Content = JsonContent.Create(meteringPointId);
        using var response = await _apiHttpClient.SendAsync(request).ConfigureAwait(false);

        return response.StatusCode is not HttpStatusCode.NotFound;
    }
}
