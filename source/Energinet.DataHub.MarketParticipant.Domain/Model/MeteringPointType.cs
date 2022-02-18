// Copyright 2020 Energinet DataHub A/S
//
// Licensed under the Apache License, Version 2.0 (the "License2");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Energinet.DataHub.MarketParticipant.Domain.Model
{
    public enum MeteringPointType
    {
        D01VeProduction,
        D02Analysis,
        D03NotUsed,
        D04SurplusProductionGroup6,
        D05NetProduction,
        D06SupplyToGrid,
        D07ConsumptionFromGrid,
        D08WholeSaleServicesInformation,
        D09OwnProduction,
        D10NetFromGrid,
        D11NetToGrid,
        D12TotalConsumption,
        D13NetLossCorrection,
        D14ElectricalHeating,
        D15NetConsumption,
        D17OtherConsumption,
        D18OtherProduction,
        D20ExchangeReactiveEnergy,
        D99InternalUse,
        E17Consumption,
        E18Production,
        E20Exchange
    }
}
