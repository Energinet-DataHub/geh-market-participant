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
using System.Linq;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Authorization.Infrastructure.Persistence.Model;
using NodaTime.Extensions;

namespace Energinet.DataHub.MarketParticipant.Authorization.Infrastructure.Persistence.Mappers;

internal static class ActorMapper
{
    public static void MapToEntity(Actor from, ActorEntity to)
    {
        to.Id = from.Id.Value;
        to.OrganizationId = from.OrganizationId.Value;
        to.ActorId = from.ExternalActorId?.Value;
        to.ActorNumber = from.ActorNumber.Value;
        to.Status = from.Status;
        to.Name = from.Name.Value;

        // Market roles are currently treated as value types, so they are deleted and recreated with each update.
        var marketRoleEntity = new MarketRoleEntity
        {
            Function = from.MarketRole.Function,
            Comment = from.MarketRole.Comment
        };

        foreach (var marketRoleGridArea in from.MarketRole.GridAreas)
        {
            var gridAreaEntity = new MarketRoleGridAreaEntity
            {
                GridAreaId = marketRoleGridArea.Id.Value
            };

            foreach (var meteringPointType in marketRoleGridArea.MeteringPointTypes)
            {
                gridAreaEntity.MeteringPointTypes.Add(new MeteringPointTypeEntity
                {
                    MeteringTypeId = (int)meteringPointType
                });
            }

            marketRoleEntity.GridAreas.Add(gridAreaEntity);
        }

        to.MarketRole = marketRoleEntity;

        switch (from.Credentials)
        {
            case ActorClientSecretCredentials credentials:
                if (to.ClientSecretCredential == null ||
                    to.ClientSecretCredential.ClientSecretIdentifier != credentials.SecretIdentifier.ToString() ||
                    to.ClientSecretCredential.ExpirationDate != credentials.ExpirationDate.ToDateTimeOffset())
                {
                    to.ClientSecretCredential = new ActorClientSecretCredentialsEntity
                    {
                        ClientSecretIdentifier = credentials.SecretIdentifier.ToString(),
                        ExpirationDate = credentials.ExpirationDate.ToDateTimeOffset(),
                    };
                }

                to.CertificateCredential = null;
                break;
            case ActorCertificateCredentials credentials:
                if (to.CertificateCredential == null ||
                    to.CertificateCredential.CertificateThumbprint != credentials.CertificateThumbprint ||
                    to.CertificateCredential.KeyVaultSecretIdentifier != credentials.KeyVaultSecretIdentifier ||
                    to.CertificateCredential.ExpirationDate != credentials.ExpirationDate.ToDateTimeOffset())
                {
                    to.CertificateCredential = new ActorCertificateCredentialsEntity
                    {
                        CertificateThumbprint = credentials.CertificateThumbprint,
                        KeyVaultSecretIdentifier = credentials.KeyVaultSecretIdentifier,
                        ExpirationDate = credentials.ExpirationDate.ToDateTimeOffset(),
                    };
                }

                to.ClientSecretCredential = null;
                break;
            case null:
                to.CertificateCredential = null;
                to.ClientSecretCredential = null;
                break;
        }
    }

    public static Actor MapFromEntity(ActorEntity from)
    {
        var marketRole =
            new ActorMarketRole(
                from.MarketRole.Function,
                from.MarketRole
                    .GridAreas
                    .Select(grid => new ActorGridArea(
                        new GridAreaId(grid.GridAreaId),
                        grid.MeteringPointTypes.Select(e => (MeteringPointType)e.MeteringTypeId))).ToList(),
                from.MarketRole.Comment);

        var actorNumber = ActorNumber.Create(from.ActorNumber);
        var actorStatus = from.Status;
        var actorName = new ActorName(from.Name);

        return new Actor(
            new ActorId(from.Id),
            new OrganizationId(from.OrganizationId),
            from.ActorId.HasValue ? new ExternalActorId(from.ActorId.Value) : null,
            actorNumber,
            actorStatus,
            marketRole,
            actorName,
            MapCredentials(from));
    }

    private static ActorCredentials? MapCredentials(ActorEntity actor)
    {
        if (actor.CertificateCredential != null)
        {
            return new ActorCertificateCredentials(
                actor.CertificateCredential.CertificateThumbprint,
                actor.CertificateCredential.KeyVaultSecretIdentifier,
                actor.CertificateCredential.ExpirationDate.ToInstant());
        }

        if (actor.ClientSecretCredential != null)
        {
            return new ActorClientSecretCredentials(
                actor.ActorId!.Value,
                Guid.Parse(actor.ClientSecretCredential.ClientSecretIdentifier),
                actor.ClientSecretCredential.ExpirationDate.ToInstant());
        }

        return null;
    }
}
