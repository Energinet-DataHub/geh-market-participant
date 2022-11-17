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
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        var existingActors = to.Actors.ToDictionary(x => x.Id);
        var toAdd = new List<UserActorEntity>();
        foreach (var actor in from.Actors)
        {
            if (existingActors.TryGetValue(actor.Id, out var existing))
            {
                // TODO: Handle updates
                toAdd.Add(existing);
                continue;
            }

            var userActor = new UserActorEntity();
            MapToEntity(actor, userActor, to);
            toAdd.Add(userActor);
        }

        to.Actors.Clear();
        toAdd.ForEach(x => to.Actors.Add(x));
    }

    public static User MapFromEntity(UserEntity from)
    {
        return new User(
            from.Id,
            from.Name,
            new List<UserActor>());
    }

    private static void MapToEntity(UserActor from, UserActorEntity to, UserEntity user)
    {
        to.Id = from.Id;
        to.ActorId = from.ActorId;
        to.UserId = user.Id;
        var existingRoles = to.UserRoles.ToDictionary(x => x.Id);
        var toAdd = new List<UserActorUserRoleEntity>();
        foreach (var role in from.UserRoles)
        {
            if (existingRoles.TryGetValue(role.Id, out var existing))
            {
                toAdd.Add(existing);
                continue;
            }

            toAdd.Add(MapToNewEntity(role, from));
        }

        to.UserRoles.Clear();
        toAdd.ForEach(x => to.UserRoles.Add(x));
    }

    private static void MapToEntity(UserActorUserRole from, UserActorUserRoleEntity to, UserActor userActor)
    {
        to.Id = from.Id;
        to.UserActorId = userActor.Id;
        //to.UserRoleTemplate
    }

    private static UserActorUserRoleEntity MapToNewEntity(UserActorUserRole from, UserActor userActor)
    {
        var newEntity = new UserActorUserRoleEntity(MapToNewEntity(from.UserRole))
        {
            Id = from.Id, UserActorId = userActor.Id
        };
        return newEntity;
    }

    private static UserRoleTemplateEntity MapToNewEntity(UserRoleTemplate from)
    {
        var newEntity = new UserRoleTemplateEntity(from.Name)
        {
            Id = Guid.Empty, Permissions = new Collection<UserRoleTemplatePermissionEntity>(from.Permissions.Select(MapToNewEntity).ToList())
        };

        return newEntity;
    }

    private static UserRoleTemplatePermissionEntity MapToNewEntity(Permission from)
    {
        return new UserRoleTemplatePermissionEntity(new PermissionEntity(from.Id, from.Description), Guid.Empty);
    }
}
