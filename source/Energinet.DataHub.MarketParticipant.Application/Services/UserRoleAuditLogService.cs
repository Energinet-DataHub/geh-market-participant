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
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;

namespace Energinet.DataHub.MarketParticipant.Application.Services
{
    public class UserRoleAuditLogService : IUserRoleAuditLogService
    {
        public IEnumerable<UserRoleAuditLogEntry> BuildAuditLogsForUserRoleCreated(
            UserId currentUserId,
            UserRoleId userRoleId,
            UserRole userRoleUpdate)
        {
            ArgumentNullException.ThrowIfNull(currentUserId, nameof(currentUserId));
            ArgumentNullException.ThrowIfNull(userRoleId, nameof(userRoleId));
            ArgumentNullException.ThrowIfNull(userRoleUpdate, nameof(userRoleUpdate));

            yield return UserRoleAuditLogEntry(currentUserId, userRoleId, userRoleUpdate, UserRoleChangeType.Created);
        }

        public IEnumerable<UserRoleAuditLogEntry> BuildAuditLogsForUserRoleChanged(
            UserId currentUserId,
            UserRole userRole,
            UserRole userRoleUpdate)
        {
            ArgumentNullException.ThrowIfNull(currentUserId, nameof(currentUserId));
            ArgumentNullException.ThrowIfNull(userRole, nameof(userRole));
            ArgumentNullException.ThrowIfNull(userRoleUpdate, nameof(userRoleUpdate));

            if (userRole.Name != userRoleUpdate.Name)
            {
                yield return UserRoleAuditLogEntry(currentUserId, userRole.Id, userRoleUpdate, UserRoleChangeType.NameChange);
            }

            if (userRole.Description != userRoleUpdate.Description)
            {
                yield return UserRoleAuditLogEntry(currentUserId, userRole.Id, userRoleUpdate, UserRoleChangeType.DescriptionChange);
            }

            if (userRole.EicFunction != userRoleUpdate.EicFunction)
            {
                yield return UserRoleAuditLogEntry(currentUserId, userRole.Id, userRoleUpdate, UserRoleChangeType.EicFunctionChange);
            }

            if (userRole.Status != userRoleUpdate.Status)
            {
                yield return UserRoleAuditLogEntry(currentUserId, userRole.Id, userRoleUpdate, UserRoleChangeType.StatusChange);
            }

            if (!userRole.Permissions.Select(e => e.ToString()).ToList().SequenceEqual(userRoleUpdate.Permissions.Select(e => e.ToString()).ToList()))
            {
                yield return UserRoleAuditLogEntry(currentUserId, userRole.Id, userRoleUpdate, UserRoleChangeType.PermissionsChange);
            }
        }

        private static UserRoleAuditLogEntry UserRoleAuditLogEntry(
            UserId currentUserId,
            UserRoleId userRoleId,
            UserRole userRoleUpdate,
            UserRoleChangeType changeType)
        {
            return new UserRoleAuditLogEntry(
                userRoleId,
                currentUserId,
                DateTimeOffset.UtcNow,
                changeType,
                BuildChangeDescriptionJson(
                    changeType,
                    userRoleUpdate));
        }

        private static string BuildChangeDescriptionJson(
            UserRoleChangeType userRoleChangeType,
            UserRole userRoleUpdate)
        {
            return userRoleChangeType switch
            {
                UserRoleChangeType.Created => SerializeObject(MapToUserRoleAuditLogSerialized(userRoleUpdate)),
                UserRoleChangeType.NameChange => SerializeObject(new UserRoleAuditLogSerialized { Name = userRoleUpdate.Name }),
                UserRoleChangeType.DescriptionChange => SerializeObject(new UserRoleAuditLogSerialized { Description = userRoleUpdate.Description }),
                UserRoleChangeType.EicFunctionChange => SerializeObject(new UserRoleAuditLogSerialized { EicFunction = userRoleUpdate.EicFunction }),
                UserRoleChangeType.StatusChange => SerializeObject(new UserRoleAuditLogSerialized { Status = userRoleUpdate.Status }),
                UserRoleChangeType.PermissionsChange => SerializeObject(new UserRoleAuditLogSerialized { Permissions = userRoleUpdate.Permissions.Select(e => (int)e) }),
                _ => throw new ArgumentOutOfRangeException(nameof(userRoleChangeType))
            };
        }

        private static string SerializeObject(UserRoleAuditLogSerialized objectToSerialize)
        {
            return JsonSerializer.Serialize(
                objectToSerialize,
                new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });
        }

        private static UserRoleAuditLogSerialized MapToUserRoleAuditLogSerialized(UserRole from)
        {
            return new UserRoleAuditLogSerialized()
            {
                Name = from.Name,
                Description = from.Description,
                EicFunction = from.EicFunction,
                Status = from.Status,
                Permissions = from.Permissions.Select(e => (int)e)
            };
        }
    }
}
