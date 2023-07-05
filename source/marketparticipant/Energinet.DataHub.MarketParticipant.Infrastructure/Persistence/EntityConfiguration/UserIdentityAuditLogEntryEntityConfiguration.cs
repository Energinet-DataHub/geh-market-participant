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
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.EntityConfiguration
{
    public class UserIdentityAuditLogEntryEntityConfiguration : IEntityTypeConfiguration<UserIdentityAuditLogEntryEntity>
    {
        public void Configure(EntityTypeBuilder<UserIdentityAuditLogEntryEntity> builder)
        {
            ArgumentNullException.ThrowIfNull(builder, nameof(builder));

            builder.ToTable("UserIdentityAuditLogEntry");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedOnAdd();
            builder.Property(x => x.UserId);
            builder.Property(x => x.Field);
            builder.Property(x => x.NewValue);
            builder.Property(x => x.OldValue);
            builder.Property(x => x.ChangedByUserId);
            builder.Property(x => x.Timestamp);
        }
    }
}
