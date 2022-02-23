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
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Energinet.DataHub.MarketParticipant.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.EntityConfiguration
{
    public sealed class OrganizationRoleEntityConfiguration : IEntityTypeConfiguration<OrganizationRoleEntity>
    {
        public void Configure(EntityTypeBuilder<OrganizationRoleEntity> builder)
        {
            Guard.ThrowIfNull(builder, nameof(builder));
            builder.ToTable("OrganizationRole");
            builder.HasKey(role => role.Id);
            builder.Property(role => role.Id).ValueGeneratedOnAdd();
            builder.OwnsMany(role => role.MeteringPointTypes, ConfigureMeteringTypes);
        }

        private void ConfigureMeteringTypes(
            OwnedNavigationBuilder<OrganizationRoleEntity, MeteringPointType> meteringPointTypeBuilder)
        {
            meteringPointTypeBuilder.WithOwner().HasForeignKey("OrganizationRoleId");
            meteringPointTypeBuilder.ToTable("OrganizationRoleMeteringType");
            meteringPointTypeBuilder.Property<Guid>("Id").ValueGeneratedOnAdd();
            meteringPointTypeBuilder.Property(p => p.Value).HasColumnName("MeteringTypeId");
        }
    }
}
