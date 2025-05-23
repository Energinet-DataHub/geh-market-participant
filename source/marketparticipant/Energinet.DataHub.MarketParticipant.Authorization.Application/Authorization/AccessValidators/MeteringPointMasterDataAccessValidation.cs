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
using Energinet.DataHub.MarketParticipant.Authorization.Application.Authorization.Clients;
using Energinet.DataHub.MarketParticipant.Authorization.Model;
using Energinet.DataHub.MarketParticipant.Authorization.Model.AccessValidationRequests;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using EicFunction = Energinet.DataHub.MarketParticipant.Authorization.Model.EicFunction;


namespace Energinet.DataHub.MarketParticipant.Authorization.Application.Authorization.AccessValidators;

public sealed class MeteringPointMasterDataAccessValidation : IAccessValidator<MeteringPointMasterDataAccessValidationRequest>
{
    private readonly IElectricityMarketClient _electricityMarketClient;
    private readonly IGridAreaOverviewRepository _gridAreaRepository;

    public MeteringPointMasterDataAccessValidation(IElectricityMarketClient electricityMarketClient, IGridAreaOverviewRepository gridAreaRepository)
    {
        _electricityMarketClient = electricityMarketClient;
        _gridAreaRepository = gridAreaRepository;
    }

    public async Task<bool> ValidateAsync(MeteringPointMasterDataAccessValidationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return request.MarketRole switch
        {
            EicFunction.DataHubAdministrator => true,
            EicFunction.GridAccessProvider => await ValidateMeteringPointIsOfOwnedGridAreaAsync(request).ConfigureAwait(false),
            EicFunction.EnergySupplier => true,
            _ => false,
        };
    }

    private async Task<bool> ValidateMeteringPointIsOfOwnedGridAreaAsync(MeteringPointMasterDataAccessValidationRequest request)
    {
        var actorNumber = "test"; // TODO: Have this in the validation request
        var gridAreas = await _gridAreaRepository.GetAsync().ConfigureAwait(false);
        var gridAreasForGridOperator = gridAreas
            .Where(x => x.ActorNumber != null && x.ActorNumber.Value == actorNumber);

        //List of grid areas that are valid as of now.
        var validGridAreas = gridAreasForGridOperator.Where(x => x.ValidFrom >= DateTime.UtcNow && x.ValidTo >= DateTime.UtcNow).Select(g => new List<string> { g.Code.Value });
        //TODO: Make a call to new Electricity market api specially for the signature creation.
        return await _electricityMarketClient.GetMeteringPointMasterDataForGridAccessProviderAllowedAsync(request.MeteringPointId, (List<string>)validGridAreas).ConfigureAwait(false);
    }
}
