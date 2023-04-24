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

using System.Linq;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Mappers;

internal static class UserMapper
{
    public static void MapToEntity(User from, UserEntity to)
    {
        to.Id = from.Id.Value;
        to.ExternalId = from.ExternalId.Value;
        to.MitIdSignupInitiatedAt = from.MitIdSignupInitiatedTimestampAt;

        var newAssignments = from
            .RoleAssignments
            .Select(newRa =>
            {
                var existing = to.RoleAssignments
                    .FirstOrDefault(oldRa => oldRa.ActorId == newRa.ActorId.Value && oldRa.UserRoleId == newRa.UserRoleId.Value);

                return existing ?? MapToEntity(newRa, from.Id);
            })
            .ToList();

        to.RoleAssignments.Clear();

        foreach (var userRoleAssignment in newAssignments)
        {
            to.RoleAssignments.Add(userRoleAssignment);
        }
    }

    public static User MapFromEntity(UserEntity from)
    {
        return new User(
            new UserId(from.Id),
            new ExternalUserId(from.ExternalId),
            from.RoleAssignments.Select(MapFromEntity),
            from.MitIdSignupInitiatedAt);
    }

    private static UserRoleAssignmentEntity MapToEntity(UserRoleAssignment fromRoleAssignment, UserId fromId)
    {
        return new UserRoleAssignmentEntity
        {
            ActorId = fromRoleAssignment.ActorId.Value,
            UserId = fromId.Value,
            UserRoleId = fromRoleAssignment.UserRoleId.Value
        };
    }

    private static UserRoleAssignment MapFromEntity(UserRoleAssignmentEntity from)
    {
        return new UserRoleAssignment(new ActorId(from.ActorId), new UserRoleId(from.UserRoleId));
    }
}
