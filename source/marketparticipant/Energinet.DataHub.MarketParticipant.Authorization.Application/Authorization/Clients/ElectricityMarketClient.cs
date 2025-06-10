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

using System.Net.Http.Json;
using Energinet.DataHub.MarketParticipant.Authorization.Model;
using NodaTime;

namespace Energinet.DataHub.MarketParticipant.Authorization.Application.Authorization.Clients;

public sealed class ElectricityMarketClient : IElectricityMarketClient
{
    private readonly HttpClient _apiHttpClient;

    public ElectricityMarketClient(HttpClient apiHttpClient)
    {
        _apiHttpClient = apiHttpClient;
    }

    public async Task<bool> VerifyMeteringPointIsInGridAreaAsync(string meteringPointId, IEnumerable<string> gridAreaCodes)
    {
        ArgumentNullException.ThrowIfNull(meteringPointId);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/metering-point/verify-grid-owner?identification={meteringPointId}");
        request.Content = JsonContent.Create(gridAreaCodes);

        using var response = await _apiHttpClient.SendAsync(request).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<bool>().ConfigureAwait(false);
    }

    public async Task<IEnumerable<AccessPeriod>> GetSupplierPeriodsAsync(string meteringPointId, string actorNumber, Interval requestedPeriod)
    {
        ArgumentNullException.ThrowIfNull(meteringPointId);
        ArgumentNullException.ThrowIfNull(actorNumber);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/metering-point/get-supplier-periods?meteringPointId={meteringPointId}&actorNumber={actorNumber}");
        request.Content = JsonContent.Create(requestedPeriod);

        using var response = await _apiHttpClient.SendAsync(request).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<Interval>().ConfigureAwait(false);

        return await response.Content.ReadFromJsonAsync<IEnumerable<AccessPeriod>>().ConfigureAwait(false);
    }
}
