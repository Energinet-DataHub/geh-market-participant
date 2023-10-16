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
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Audit;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.EntityConfiguration
{
    public sealed class OrganizationEntityConfiguration : AuditedEntityTypeConfiguration<OrganizationEntity>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<OrganizationEntity> builder)
        {
            ArgumentNullException.ThrowIfNull(builder, nameof(builder));
            builder.ToTable("Organization");
            builder.HasKey(organization => organization.Id);
            builder.Property(organization => organization.Id).ValueGeneratedOnAdd();
            builder.Property(organization => organization.City).HasColumnName("Address_City");
            builder.Property(organization => organization.StreetName).HasColumnName("Address_StreetName");
            builder.Property(organization => organization.Number).HasColumnName("Address_Number");
            builder.Property(organization => organization.ZipCode).HasColumnName("Address_ZipCode");
            builder.Property(organization => organization.Country).HasColumnName("Address_Country");
        }
    }
}
