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

using Energinet.DataHub.MarketParticipant.Authorization.Application.Authorization.Clients;
using Energinet.DataHub.MarketParticipant.Authorization.Model.AccessValidationRequests;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using EicFunction = Energinet.DataHub.MarketParticipant.Authorization.Model.EicFunction;

namespace Energinet.DataHub.MarketParticipant.Authorization.Application.Authorization.AccessValidators;

public sealed class MeteringPointYearlySumMeasurementDataAccessValidation : IAccessValidator<MeteringPointMeasurementDataAccessValidationRequest>
{
    private readonly IElectricityMarketClient _electricityMarketClient;
    private readonly IGridAreaOverviewRepository _gridAreaRepository;

    public MeteringPointYearlySumMeasurementDataAccessValidation(IElectricityMarketClient electricityMarketClient, IGridAreaOverviewRepository gridAreaRepository)
    {
        _electricityMarketClient = electricityMarketClient;
        _gridAreaRepository = gridAreaRepository;
    }

    public async Task<AccessValidatorResponse> ValidateAsync(MeteringPointMeasurementDataAccessValidationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return request.MarketRole switch
        {
            EicFunction.EnergySupplier => await IsAllowedForBalanceSupplierAsync(request).ConfigureAwait(false),
            _ => new AccessValidatorResponse(false, null)
        };
    }

    private static async Task<AccessValidatorResponse> IsAllowedForBalanceSupplierAsync(MeteringPointMeasurementDataAccessValidationRequest request)
    {
        //TODO in next task implement validation rules
        return new AccessValidatorResponse(false, null);
    }

}
