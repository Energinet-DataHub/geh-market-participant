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
    public sealed class ActorEntityConfiguration : IEntityTypeConfiguration<ActorEntity>
    {
        public void Configure(EntityTypeBuilder<ActorEntity> builder)
        {
            Guard.ThrowIfNull(builder, nameof(builder));
            builder.ToTable("ActorInfoNew");
            builder.HasKey(actor => actor.Id);
            builder.Property(actor => actor.Id).ValueGeneratedOnAdd();
            builder
                .HasOne(actor => actor.SingleGridArea!)
                .WithMany()
                .HasForeignKey("GridAreaId");
            builder
                .HasMany(actor => actor.MarketRoles)
                .WithOne()
                .HasForeignKey(marketRole => marketRole.ActorInfoId);
            builder.OwnsMany(role => role.MeteringPointTypes, ConfigureMeteringTypes);
        }

        private static void ConfigureMeteringTypes(
            OwnedNavigationBuilder<ActorEntity, MeteringPointType> meteringPointTypeBuilder)
        {
            meteringPointTypeBuilder.WithOwner().HasForeignKey("ActorInfoId");
            meteringPointTypeBuilder.ToTable("ActorInfoMeteringType");
            meteringPointTypeBuilder.Property<Guid>("Id").ValueGeneratedOnAdd();
            meteringPointTypeBuilder.Property(p => p.Value).HasColumnName("MeteringTypeId");
        }
    }
}
