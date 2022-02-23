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

using Ardalis.SmartEnum;

namespace Energinet.DataHub.MarketParticipant.Domain.Model
{
    public sealed class MeteringPointType : SmartEnum<MeteringPointType>
    {
        public static readonly MeteringPointType Unknown =
            new MeteringPointType(nameof(Unknown), 0);

        public static readonly MeteringPointType D01VeProduction =
            new MeteringPointType(nameof(D01VeProduction), 1);

        public static readonly MeteringPointType D02Analysis =
            new MeteringPointType(nameof(D02Analysis), 2);

        public static readonly MeteringPointType D03NotUsed =
            new MeteringPointType(nameof(D03NotUsed), 3);

        public static readonly MeteringPointType D04SurplusProductionGroup6 =
            new MeteringPointType(nameof(D04SurplusProductionGroup6), 4);

        public static readonly MeteringPointType D05NetProduction =
            new MeteringPointType(nameof(D05NetProduction), 5);

        public static readonly MeteringPointType D06SupplyToGrid =
            new MeteringPointType(nameof(D06SupplyToGrid), 6);

        public static readonly MeteringPointType D07ConsumptionFromGrid =
            new MeteringPointType(nameof(D07ConsumptionFromGrid), 7);

        public static readonly MeteringPointType D08WholeSaleServicesInformation =
            new MeteringPointType(nameof(D08WholeSaleServicesInformation), 8);

        public static readonly MeteringPointType D09OwnProduction =
            new MeteringPointType(nameof(D09OwnProduction), 9);

        public static readonly MeteringPointType D10NetFromGrid =
            new MeteringPointType(nameof(D10NetFromGrid), 10);

        public static readonly MeteringPointType D11NetToGrid =
            new MeteringPointType(nameof(D11NetToGrid), 11);

        public static readonly MeteringPointType D12TotalConsumption =
            new MeteringPointType(nameof(D12TotalConsumption), 12);

        public static readonly MeteringPointType D13NetLossCorrection =
            new MeteringPointType(nameof(D13NetLossCorrection), 13);

        public static readonly MeteringPointType D14ElectricalHeating =
            new MeteringPointType(nameof(D14ElectricalHeating), 14);

        public static readonly MeteringPointType D15NetConsumption =
            new MeteringPointType(nameof(D15NetConsumption), 15);

        public static readonly MeteringPointType D17OtherConsumption =
            new MeteringPointType(nameof(D17OtherConsumption), 16);

        public static readonly MeteringPointType D18OtherProduction =
            new MeteringPointType(nameof(D18OtherProduction), 17);

        public static readonly MeteringPointType D20ExchangeReactiveEnergy =
            new MeteringPointType(nameof(D20ExchangeReactiveEnergy), 18);

        public static readonly MeteringPointType D99InternalUse =
            new MeteringPointType(nameof(D99InternalUse), 19);

        public static readonly MeteringPointType E17Consumption =
            new MeteringPointType(nameof(E17Consumption), 20);

        public static readonly MeteringPointType E18Production =
            new MeteringPointType(nameof(E18Production), 21);

        public static readonly MeteringPointType E20Exchange =
            new MeteringPointType(nameof(E20Exchange), 22);

        private MeteringPointType(string name, int value)
            : base(name, value)
        {
        }

        private MeteringPointType()
            : base("default", -1)
        {
        }
    }
}
