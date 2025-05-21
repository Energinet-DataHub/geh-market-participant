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




using Energinet.DataHub.MarketParticipant.Authorization.Application;
using Energinet.DataHub.MarketParticipant.Authorization.Application.Models.MasterData;
using Energinet.DataHub.MarketParticipant.Authorization.Model;
using Energinet.DataHub.MarketParticipant.Authorization.Model.AccessValidationRequests;
using NodaTime;


namespace Energinet.DataHub.MarketParticipant.Authorization.Application.Authorization.AccessValidators;

public sealed class MeteringPointMasterDataAccessValidation : IAccessValidator
{
    //private readonly HttpClient _apiHttpClient;
    //private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions().ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
    //internal MeteringPointMasterDataAccessValidation(HttpClient apiHttpClient)
    //{
    //    _apiHttpClient = apiHttpClient;
    //}

    //internal MeteringPointMasterDataAccessValidation()
    //{
    //    _apiHttpClient = new HttpClient();
    //    _apiHttpClient.BaseAddress = new Uri("test.test");
    //}
    public MeteringPointMasterDataAccessValidation(MeteringPointMasterDataAccessValidationRequest validationRequest)
    {
        ArgumentNullException.ThrowIfNull(validationRequest);

        MarketRole = validationRequest.MarketRole;
    }

    public EicFunction MarketRole { get; }

    public bool Validate()
    {
        return MarketRole switch
        {
            EicFunction.DataHubAdministrator => true,
            EicFunction.GridAccessProvider => ValidateMeteringPointIsOfOwnedGridArea(),
            EicFunction.EnergySupplier => true,
            _ => false,
        };
    }

    private bool ValidateMeteringPointIsOfOwnedGridArea()
    {
        //TODO: Call elecitricity market
        var marketRole = MarketRole;
        //var electricityMarket = new ElectricityMarket(( new MeteringPointIdentification() {  Value='123'}, interval);

        //USE GLN and market role for look up Grid Area
        //lookup metering point to compare the registered grid
        return false;
    }
}
