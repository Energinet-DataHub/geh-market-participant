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
using Energinet.DataHub.MarketParticipant.Authorization.AccessValidation;
using Energinet.DataHub.MarketParticipant.Authorization.Model;
using Energinet.DataHub.MarketParticipant.Authorization.Model.MasterData;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;

namespace Energinet.DataHub.MarketParticipant.Authorization.Services.AccessValidators;

public sealed class MeteringPointMasterDataAccessValidation : IAccessValidator, IDisposable
{
    private readonly HttpClient _apiHttpClient;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions().ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
    //internal MeteringPointMasterDataAccessValidation(HttpClient apiHttpClient)
    //{
    //    _apiHttpClient = apiHttpClient;
    //}

    //internal MeteringPointMasterDataAccessValidation()
    //{
    //    _apiHttpClient = new HttpClient();
    //    _apiHttpClient.BaseAddress = new Uri("test.test");
    //}
    private MeteringPointMasterDataAccessValidation(MeteringPointMasterDataAccessValidationRequest validationRequest)
    {
        ArgumentNullException.ThrowIfNull(validationRequest);

        MarketRole = validationRequest.MarketRole;
        _apiHttpClient = new HttpClient();
        _apiHttpClient.BaseAddress = new Uri("test.test");
    }

    public EicFunction MarketRole { get; }

    public void Dispose()
    {
        _apiHttpClient.Dispose();
    }

    public bool Validate()
    {
        return MarketRole switch
        {
            EicFunction.DataHubAdministrator => true,
            EicFunction.GridAccessProvider => ValidateMeteringPointIsOfOwnedGridArea(),
           _ => false,
        };
    }

    private bool ValidateMeteringPointIsOfOwnedGridArea()
    {
        //TODO: Call elecitricity market
        var marketRole = MarketRole;
        //USE GLN and market role for look up Grid Area
        //lookup metering point to compare the registered grid
        return false;
    }

    private async Task<IEnumerable<MeteringPointMasterData>> GetMeteringPointMasterDataChangesAsync(
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
