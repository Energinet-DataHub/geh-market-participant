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
    public sealed class ContactCategory : SmartEnum<ContactCategory>
    {
        public static readonly ContactCategory Default =
            new ContactCategory(0);

        public static readonly ContactCategory Charges =
            new ContactCategory(1);

        public static readonly ContactCategory ChargeLinks =
            new ContactCategory(2);

        public static readonly ContactCategory ElectricalHeating =
            new ContactCategory(3);

        public static readonly ContactCategory EndOfSupply =
            new ContactCategory(4);

        public static readonly ContactCategory EnerginetInquiry =
            new ContactCategory(5);

        public static readonly ContactCategory ErrorReport =
            new ContactCategory(6);

        public static readonly ContactCategory IncorrectMove =
            new ContactCategory(7);

        public static readonly ContactCategory IncorrectSwitch =
            new ContactCategory(8);

        public static readonly ContactCategory MeasurementData =
            new ContactCategory(9);

        public static readonly ContactCategory MeteringPoint =
            new ContactCategory(10);

        public static readonly ContactCategory NetSettlement =
            new ContactCategory(11);

        public static readonly ContactCategory Notification =
            new ContactCategory(12);

        public static readonly ContactCategory Recon =
            new ContactCategory(13);

        public static readonly ContactCategory Reminder =
            new ContactCategory(14);

        private ContactCategory(int value, [CallerMemberName] string name = "")
            : base(name, value)
        {
        }

        private ContactCategory()
            : base("default", -1)
        {
        }
    }
}
