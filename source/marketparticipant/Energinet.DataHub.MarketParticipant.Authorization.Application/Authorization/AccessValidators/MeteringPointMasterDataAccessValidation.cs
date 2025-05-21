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
using Energinet.DataHub.MarketParticipant.Authorization.Application.Authorization.Clients;
using Energinet.DataHub.MarketParticipant.Authorization.Model;
using Energinet.DataHub.MarketParticipant.Authorization.Model.AccessValidationRequests;
using NodaTime;


namespace Energinet.DataHub.MarketParticipant.Authorization.Application.Authorization.AccessValidators;

public sealed class MeteringPointMasterDataAccessValidation : IAccessValidator
{
    private readonly IElectricityMarketClient _electricityMarketClient;
    private readonly MeteringPointMasterDataAccessValidationRequest _validationRequest;

    public MeteringPointMasterDataAccessValidation(MeteringPointMasterDataAccessValidationRequest validationRequest, IElectricityMarketClient electricityMarketClient)
    {
        ArgumentNullException.ThrowIfNull(validationRequest);
        _electricityMarketClient = electricityMarketClient;
        _validationRequest = validationRequest;
    }

    public async Task<bool> ValidateAsync()
    {
        return _validationRequest.MarketRole switch
        {
            EicFunction.DataHubAdministrator => true,
            EicFunction.GridAccessProvider => await ValidateMeteringPointIsOfOwnedGridAreaAsync().ConfigureAwait(false),
            EicFunction.EnergySupplier => true,
            _ => false,
        };
    }

    private async Task<bool> ValidateMeteringPointIsOfOwnedGridAreaAsync()
    {
       return await _electricityMarketClient.GetMeteringPointMasterDataAsync(_validationRequest.MeteringPointId).ConfigureAwait(false);
    }
}
