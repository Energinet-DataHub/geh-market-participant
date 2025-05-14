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

using System.Collections.Generic;
using NodaTime;

namespace Energinet.DataHub.MarketParticipant.Authorization.Model.MasterData
{
    public sealed class MeteringPointMasterData
    {
        public MeteringPointIdentification Identification { get; init; } = null!;

        public Instant ValidFrom { get; init; }

        public Instant ValidTo { get; init; }

        public GridAreaCode GridAreaCode { get; init; } = null!;

        public string GridAccessProvider { get; init; } = null!;

        public IReadOnlyCollection<string> NeighborGridAreaOwners { get; init; } = [];

        public string? EnergySupplier { get; init; }
    }
}
