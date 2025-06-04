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
using Energinet.DataHub.MarketParticipant.Authorization.Model;
using Energinet.DataHub.MarketParticipant.Authorization.Model.AccessValidationRequests;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using EicFunction = Energinet.DataHub.MarketParticipant.Authorization.Model.EicFunction;

namespace Energinet.DataHub.MarketParticipant.Authorization.Application.Authorization.AccessValidators;

public sealed class MeteringPointMeasurementDataAccessValidation : IAccessValidator<MeteringPointMeasurementDataAccessValidationRequest>
{
    private readonly IElectricityMarketClient _electricityMarketClient;
    private readonly IGridAreaOverviewRepository _gridAreaRepository;

    public MeteringPointMeasurementDataAccessValidation(IElectricityMarketClient electricityMarketClient, IGridAreaOverviewRepository gridAreaRepository)
    {
        _electricityMarketClient = electricityMarketClient;
        _gridAreaRepository = gridAreaRepository;
    }

    public async Task<AccessValidatorResponse> ValidateAsync(MeteringPointMeasurementDataAccessValidationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return request.MarketRole switch
        {
            EicFunction.DataHubAdministrator => IsAllowedForDataHubAdministrator(request),
            EicFunction.GridAccessProvider => await IsAllowedForGridAccessProviderAsync(request).ConfigureAwait(false),
            EicFunction.EnergySupplier => await IsAllowedForBalanceSupplierAsync(request).ConfigureAwait(false),
            _ => new AccessValidatorResponse(false, null)
        };
    }

    private static AccessValidatorResponse IsAllowedForDataHubAdministrator(MeteringPointMeasurementDataAccessValidationRequest request)
    {
        // For administrator the result is always ok for the requested period
        var accessPeriods = new List<AccessPeriod>
        {
            request.RequestedPeriod with { MeteringPointId = request.MeteringPointId }
        };
        return new AccessValidatorResponse(true, accessPeriods);
    }

    private async Task<AccessValidatorResponse> IsAllowedForBalanceSupplierAsync(MeteringPointMeasurementDataAccessValidationRequest request)
    {
        // Dummy result because no implementation of validation is created yet. This will be implemented with a new task.
        // TODO implement logic to verify metering point is allowed and requested period is allowed or provide new period within the requested period
        var accessPeriods = new List<AccessPeriod> { new(request.MeteringPointId, DateTimeOffset.UtcNow.AddDays(-90), DateTimeOffset.UtcNow.AddDays(-10)) };

        return await Task.FromResult(new AccessValidatorResponse(true, accessPeriods)).ConfigureAwait(false);
    }

    private async Task<AccessValidatorResponse> IsAllowedForGridAccessProviderAsync(MeteringPointMeasurementDataAccessValidationRequest request)
    {
        var accessPeriods = new List<AccessPeriod>() { request.RequestedPeriod };
        var valid = await ValidateMeteringPointIsOfOwnedGridAreaAsync(request).ConfigureAwait(false);
        return new AccessValidatorResponse(valid, valid ? accessPeriods : null);
    }

    private async Task<bool> ValidateMeteringPointIsOfOwnedGridAreaAsync(MeteringPointMeasurementDataAccessValidationRequest request)
    {
        var actorNumber = request.ActorNumber;
        var gridAreas = await _gridAreaRepository.GetAsync().ConfigureAwait(false);

        var activeGridAreasCodes = gridAreas
            .Where(x => x.ActorNumber != null
                        && x.ActorNumber.Value == actorNumber
                        && x.ValidFrom <= DateTime.UtcNow && x.ValidTo >= DateTime.UtcNow)
            .Select(g => g.Code.Value)
            .ToList();
        return await _electricityMarketClient.VerifyMeteringPointIsInGridAreaAsync(request.MeteringPointId, activeGridAreasCodes).ConfigureAwait(false);
    }
}
