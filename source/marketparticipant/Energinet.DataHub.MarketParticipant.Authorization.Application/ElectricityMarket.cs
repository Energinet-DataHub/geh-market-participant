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
using System.Text.Json;
using Energinet.DataHub.MarketParticipant.Authorization.Application.Models.MasterData;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;

namespace Energinet.DataHub.MarketParticipant.Authorization.Application;

public sealed class ElectricityMarket : IElectricityMarket
{
    private readonly HttpClient _apiHttpClient;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions().ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);

    internal ElectricityMarket(HttpClient apiHttpClient)
    {
        _apiHttpClient = apiHttpClient;
    }

    public async Task<IEnumerable<MeteringPointMasterData>> GetMeteringPointMasterDataAsync(
        MeteringPointIdentification meteringPointId,
        Interval period)
    {
        ArgumentNullException.ThrowIfNull(meteringPointId);

        var f = period.Start.ToDateTimeOffset();
        var t = period.End.ToDateTimeOffset();

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/get-metering-point-master-data");
        request.Content = JsonContent.Create(new MeteringPointMasterDataRequestDto(meteringPointId.Value, f, t));
        using var response = await _apiHttpClient.SendAsync(request).ConfigureAwait(false);

        if (response.StatusCode is HttpStatusCode.NotFound)
            return [];

        var result = await response.Content
            .ReadFromJsonAsync<IEnumerable<MeteringPointMasterData>>(_jsonSerializerOptions)
            .ConfigureAwait(false) ?? [];

        return result;
    }
}
