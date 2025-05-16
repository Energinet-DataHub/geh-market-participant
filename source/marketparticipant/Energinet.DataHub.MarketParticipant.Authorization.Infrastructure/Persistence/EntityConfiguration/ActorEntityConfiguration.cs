﻿// Copyright 2020 Energinet DataHub A/S
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
using Energinet.DataHub.MarketParticipant.Authorization.Infrastructure.Persistence.Audit;
using Energinet.DataHub.MarketParticipant.Authorization.Infrastructure.Persistence.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Energinet.DataHub.MarketParticipant.Authorization.Infrastructure.Persistence.EntityConfiguration;

public class ActorEntityConfiguration : AuditedEntityTypeConfiguration<ActorEntity>
{
    protected override void ConfigureEntity(EntityTypeBuilder<ActorEntity> builder)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));
        builder.ToTable("Actor");
        builder.HasKey(actor => actor.Id);
        builder.Property(actor => actor.Id).ValueGeneratedOnAdd();
        builder
            .HasOne(actor => actor.MarketRole)
            .WithOne()
            .HasForeignKey<MarketRoleEntity>(marketRole => marketRole.ActorId);
        builder
            .Navigation(x => x.MarketRole).AutoInclude();
        builder
            .HasOne(actor => actor.CertificateCredential)
            .WithOne()
            .HasForeignKey<ActorCertificateCredentialsEntity>(cred => cred.ActorId);
        builder
            .HasOne(actor => actor.ClientSecretCredential)
            .WithOne()
            .HasForeignKey<ActorClientSecretCredentialsEntity>(cred => cred.ActorId);
        builder.Navigation(actor => actor.CertificateCredential).AutoInclude();
        builder.Navigation(actor => actor.ClientSecretCredential).AutoInclude();
        builder
            .HasMany(actor => actor.UsedActorCertificates)
            .WithOne()
            .HasForeignKey(usedCert => usedCert.ActorId);
    }
}
