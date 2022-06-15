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

using System;
using System.Collections.ObjectModel;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model
{
    public sealed class ActorEntity
    {
        public ActorEntity()
        {
            ActorNumber = string.Empty;
            MeteringPointTypes = new Collection<MeteringPointTypeEntity>();
            MarketRoles = new Collection<MarketRoleEntity>();
            GridAreas = new Collection<GridAreaActorInfoLinkEntity>();
        }

        public Guid Id { get; set; }
        public Guid? ActorId { get; set; }
        public string ActorNumber { get; set; }
        public int Status { get; set; }

        public Collection<MeteringPointTypeEntity> MeteringPointTypes { get; }
        public Collection<MarketRoleEntity> MarketRoles { get; }
        public Collection<GridAreaActorInfoLinkEntity> GridAreas { get; }

        public Guid OrganizationId { get; set; }
    }
}
