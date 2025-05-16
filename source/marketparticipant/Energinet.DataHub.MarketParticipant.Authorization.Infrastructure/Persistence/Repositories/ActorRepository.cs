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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Authorization.Infrastructure.Persistence.Mappers;
using Energinet.DataHub.MarketParticipant.Authorization.Infrastructure.Persistence.Model;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.MarketParticipant.Authorization.Infrastructure.Persistence.Repositories;

public sealed class ActorRepository(IMarketParticipantDbContext marketParticipantDbContext, IEntityLock entityLock) : IActorRepository
{
    public async Task<Result<ActorId, ActorError>> AddOrUpdateAsync(Actor actor)
    {
        ArgumentNullException.ThrowIfNull(actor);

        ActorEntity destination;

        if (actor.Id.Value == default)
        {
            entityLock.EnsureLocked(LockableEntity.Actor);
            destination = new ActorEntity();
        }
        else
        {
            destination = await marketParticipantDbContext
                .Actors
                .FindAsync(actor.Id.Value)
                .ConfigureAwait(false) ?? throw new InvalidOperationException($"Actor with id {actor.Id.Value} is missing, even though it cannot be deleted.");
        }

        if (actor.Credentials is ActorCertificateCredentials certificateCredentials &&
            destination.CertificateCredential?.CertificateThumbprint != certificateCredentials.CertificateThumbprint)
        {
            var certificateReUsedByCurrentActor = await marketParticipantDbContext
                .UsedActorCertificates
                .SingleOrDefaultAsync(e => e.Thumbprint == certificateCredentials.CertificateThumbprint && e.ActorId == destination.Id)
                .ConfigureAwait(false);

            if (certificateReUsedByCurrentActor is null)
            {
                destination.UsedActorCertificates.Add(new UsedActorCertificatesEntity
                {
                    Thumbprint = certificateCredentials.CertificateThumbprint,
                    AddedAt = DateTimeOffset.UtcNow
                });
            }
        }

        ActorMapper.MapToEntity(actor, destination);
        marketParticipantDbContext.Actors.Update(destination);

        try
        {
            await marketParticipantDbContext.SaveChangesAsync().ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is SqlException inner &&
            (inner.Message.Contains("UQ_ActorCertificateCredentials_Thumbprint", StringComparison.InvariantCultureIgnoreCase) ||
             inner.Message.Contains("UQ_UsedActorCertificates_Thumbprint", StringComparison.InvariantCultureIgnoreCase)))
        {
            return new(ActorError.ThumbprintCredentialsConflict);
        }

        return new(new ActorId(destination.Id));
    }

    public async Task<Actor?> GetAsync(ActorId actorId)
    {
        var foundActor = await marketParticipantDbContext
            .Actors
            .FirstOrDefaultAsync(actor => actor.Id == actorId.Value)
            .ConfigureAwait(false);

        return foundActor == null
            ? null
            : ActorMapper.MapFromEntity(foundActor);
    }

    public async Task<IEnumerable<Actor>> GetActorsAsync()
    {
        var actors = await marketParticipantDbContext
            .Actors
            .ToListAsync()
            .ConfigureAwait(false);

        return actors.Select(ActorMapper.MapFromEntity);
    }

    public async Task<IEnumerable<Actor>> GetActorsAsync(IEnumerable<ActorId> actorIds)
    {
        var ids = actorIds
            .Select(id => id.Value)
            .Distinct()
            .ToList();

        var query =
            from actor in marketParticipantDbContext.Actors
            where ids.Contains(actor.Id)
            select actor;

        var actors = await query
            .ToListAsync()
            .ConfigureAwait(false);

        return actors.Select(ActorMapper.MapFromEntity);
    }

    public async Task<IEnumerable<Actor>> GetActorsAsync(OrganizationId organizationId)
    {
        var query =
            from actor in marketParticipantDbContext.Actors
            where actor.OrganizationId == organizationId.Value
            select actor;

        var actors = await query
            .ToListAsync()
            .ConfigureAwait(false);

        return actors.Select(ActorMapper.MapFromEntity);
    }

    public async Task<ActorTokenData?> GetActorTokenDataAsync(ActorId actorId)
    {
        var query = from a in marketParticipantDbContext.Actors
                    join mr in marketParticipantDbContext.MarketRoles on a.Id equals mr.ActorId into actorMarketRoles
                    from mr in actorMarketRoles.DefaultIfEmpty()
                    join mrga in marketParticipantDbContext.MarketRoleGridAreas on mr.Id equals mrga.MarketRoleId into marketRoleGridAreas
                    from mrga in marketRoleGridAreas.DefaultIfEmpty()
                    join ga in marketParticipantDbContext.GridAreas on mrga.GridAreaId equals ga.Id into gridAreas
                    from ga in gridAreas.DefaultIfEmpty()
                    where a.Id == actorId.Value
                    group new
                    {
                        mr,
                        ga,
                    }
                        by a
            into grp
                    select new ActorTokenData
                    {
                        ActorId = grp.Key.Id,
                        ActorNumber = grp.Key.ActorNumber,
                        MarketRoles =
                            from mr in grp.Select(x => x.mr).Distinct()
                            where mr != null
                            select new ActorTokenDataMarketRole
                            {
                                Function = mr.Function,
                                GridAreas = grp.Where(x => x.mr == mr)
                                    .Select(x => new ActorTokenDataGridArea
                                    {
                                        GridAreaCode = x.ga.Code,
                                    }).Distinct(),
                            },
                    };
        return await query
            .FirstOrDefaultAsync().ConfigureAwait(false);
    }
}
