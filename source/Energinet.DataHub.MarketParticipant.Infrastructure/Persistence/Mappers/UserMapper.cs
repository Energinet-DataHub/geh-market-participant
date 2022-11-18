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
using System.Linq;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Mappers;

internal sealed class UserMapper
{
    public static void MapToEntity(User from, UserEntity to)
    {
        to.Id = from.Id;
        to.Name = from.Name;
        to.ExternalId = from.ExternalId;
        var existingActors = to.Actors.ToDictionary(x => x.Id);
        var toAdd = new List<UserActorEntity>();
        foreach (var actor in from.Actors)
        {
            if (existingActors.TryGetValue(actor.Id, out var existing))
            {
                MapToEntity(actor, existing);
                toAdd.Add(existing);
                continue;
            }

            var userActorEntity = new UserActorEntity();
            MapToEntity(actor, userActorEntity);
            toAdd.Add(userActorEntity);
        }

        to.Actors.Clear();
        toAdd.ForEach(x => to.Actors.Add(x));
    }

    public static User MapFromEntity(UserEntity from)
    {
        return new User(
            from.Id,
            from.Name,
            from.ExternalId,
            from.Actors.Select(MapFromEntity));
    }

    private static UserActor MapFromEntity(UserActorEntity from)
    {
        return new UserActor()
        {
            ActorId = from.ActorId,
            UserId = from.UserId,
            UserRoles = from.UserRoles.Select(MapFromEntity)
        };
    }

    private static UserActorUserRole MapFromEntity(UserActorUserRoleEntity from)
    {
        return new UserActorUserRole() { UserRoleTemplateId = from.UserRoleTemplateId };
    }

    private static void MapToEntity(UserActor from, UserActorEntity to)
    {
        to.Id = from.Id;
        to.ActorId = from.ActorId;
        to.UserId = from.UserId;
        var existingRoles = to.UserRoles.ToDictionary(x => x.Id);
        var toAdd = new List<UserActorUserRoleEntity>();
        foreach (var userRole in from.UserRoles)
        {
            if (existingRoles.TryGetValue(userRole.Id, out var existing))
            {
                MapToEntity(userRole, existing);
                toAdd.Add(existing);
                continue;
            }

            var userRoleEntity = new UserActorUserRoleEntity();
            MapToEntity(userRole, userRoleEntity);
            toAdd.Add(userRoleEntity);
        }

        to.UserRoles.Clear();
        toAdd.ForEach(x => to.UserRoles.Add(x));
    }

    private static void MapToEntity(UserActorUserRole from, UserActorUserRoleEntity to)
    {
        to.Id = from.Id;
        to.UserRoleTemplateId = from.UserRoleTemplateId;
    }
}
