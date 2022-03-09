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

using System.Runtime.CompilerServices;
using Ardalis.SmartEnum;

namespace Energinet.DataHub.MarketParticipant.Domain.Model
{
    public sealed class MeteringPointType : SmartEnum<MeteringPointType>
    {
        public static readonly MeteringPointType Unknown =
            new MeteringPointType(0);

        public static readonly MeteringPointType D01VeProduction =
            new MeteringPointType(1);

        public static readonly MeteringPointType D02Analysis =
            new MeteringPointType(2);

        public static readonly MeteringPointType D03NotUsed =
            new MeteringPointType(3);

        public static readonly MeteringPointType D04SurplusProductionGroup6 =
            new MeteringPointType(4);

        public static readonly MeteringPointType D05NetProduction =
            new MeteringPointType(5);

        public static readonly MeteringPointType D06SupplyToGrid =
            new MeteringPointType(6);

        public static readonly MeteringPointType D07ConsumptionFromGrid =
            new MeteringPointType(7);

        public static readonly MeteringPointType D08WholeSaleServicesInformation =
            new MeteringPointType(8);

        public static readonly MeteringPointType D09OwnProduction =
            new MeteringPointType(9);

        public static readonly MeteringPointType D10NetFromGrid =
            new MeteringPointType(10);

        public static readonly MeteringPointType D11NetToGrid =
            new MeteringPointType(11);

        public static readonly MeteringPointType D12TotalConsumption =
            new MeteringPointType(12);

        public static readonly MeteringPointType D13NetLossCorrection =
            new MeteringPointType(13);

        public static readonly MeteringPointType D14ElectricalHeating =
            new MeteringPointType(14);

        public static readonly MeteringPointType D15NetConsumption =
            new MeteringPointType(15);

        public static readonly MeteringPointType D17OtherConsumption =
            new MeteringPointType(16);

        public static readonly MeteringPointType D18OtherProduction =
            new MeteringPointType(17);

        public static readonly MeteringPointType D20ExchangeReactiveEnergy =
            new MeteringPointType(18);

        public static readonly MeteringPointType D99InternalUse =
            new MeteringPointType(19);

        public static readonly MeteringPointType E17Consumption =
            new MeteringPointType(20);

        public static readonly MeteringPointType E18Production =
            new MeteringPointType(21);

        public static readonly MeteringPointType E20Exchange =
            new MeteringPointType(22);

        private MeteringPointType(int value, [CallerMemberName] string name = "")
            : base(name, value)
        {
        }

        private MeteringPointType()
            : base("default", -1)
        {
        }
    }
}
