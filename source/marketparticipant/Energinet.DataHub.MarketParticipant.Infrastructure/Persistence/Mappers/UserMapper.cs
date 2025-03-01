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

using System.Linq;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Mappers;

internal static class UserMapper
{
    public static User MapFromEntity(UserEntity from)
    {
        return new User(
            new UserId(from.Id),
            new ActorId(from.AdministratedByActorId),
            new ExternalUserId(from.ExternalId),
            from.RoleAssignments.Select(MapFromEntity),
            from.MitIdSignupInitiatedAt,
            from.InvitationExpiresAt,
            from.LatestLoginAt);
    }

    private static UserRoleAssignment MapFromEntity(UserRoleAssignmentEntity from)
    {
        return new UserRoleAssignment(new ActorId(from.ActorId), new UserRoleId(from.UserRoleId));
    }
}
