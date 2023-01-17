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
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoles;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;

namespace Energinet.DataHub.MarketParticipant.Application.Services
{
    public class UserRoleAuditLogService : IUserRoleAuditLogService
    {
        public IEnumerable<UserRoleAuditLogEntry> DetermineUserRoleChangesAndBuildAuditLogs(
            UserId currentUserId,
            UserRoleWithPermissionsDto userRoleDb,
            UserRoleWithPermissionsDto userRoleUpdate)
        {
            ArgumentNullException.ThrowIfNull(currentUserId, nameof(currentUserId));
            ArgumentNullException.ThrowIfNull(userRoleDb, nameof(userRoleDb));
            ArgumentNullException.ThrowIfNull(userRoleUpdate, nameof(userRoleUpdate));

            if (string.IsNullOrEmpty(userRoleDb.Name))
            {
                return new[] { UserRoleAuditLogEntry(currentUserId, userRoleDb, userRoleUpdate, UserRoleChangeType.Created) };
            }

            var logs = new List<UserRoleAuditLogEntry>();

            if (userRoleDb.Name != userRoleUpdate.Name)
            {
                logs.Add(UserRoleAuditLogEntry(currentUserId, userRoleDb, userRoleUpdate, UserRoleChangeType.NameChange));
            }

            if (userRoleDb.Description != userRoleUpdate.Description)
            {
                logs.Add(UserRoleAuditLogEntry(currentUserId, userRoleDb, userRoleUpdate, UserRoleChangeType.DescriptionChange));
            }

            if (userRoleDb.EicFunction != userRoleUpdate.EicFunction)
            {
                logs.Add(UserRoleAuditLogEntry(currentUserId, userRoleDb, userRoleUpdate, UserRoleChangeType.EicFunctionChange));
            }

            if (userRoleDb.Status != userRoleUpdate.Status)
            {
                logs.Add(UserRoleAuditLogEntry(currentUserId, userRoleDb, userRoleUpdate, UserRoleChangeType.StatusChange));
            }

            if (!userRoleDb.Permissions.ToList().SequenceEqual(userRoleUpdate.Permissions.ToList()))
            {
                logs.Add(UserRoleAuditLogEntry(currentUserId, userRoleDb, userRoleUpdate, UserRoleChangeType.PermissionsChange));
            }

            return logs;
        }

        private static UserRoleAuditLogEntry UserRoleAuditLogEntry(
            UserId currentUserId,
            UserRoleWithPermissionsDto userRoleDb,
            UserRoleWithPermissionsDto userRoleUpdate,
            UserRoleChangeType changeType)
        {
            return new UserRoleAuditLogEntry(
                new UserRoleId(userRoleDb.Id),
                currentUserId,
                DateTimeOffset.UtcNow,
                changeType,
                BuildChangeDescriptionJson(
                    changeType,
                    userRoleUpdate));
        }

        private static string BuildChangeDescriptionJson(
            UserRoleChangeType userRoleChangeType,
            UserRoleWithPermissionsDto userRoleUpdate)
        {
            return userRoleChangeType switch
            {
                UserRoleChangeType.Created => SerializeObject(MapToUserRoleAuditLogSerialized(userRoleUpdate)),
                UserRoleChangeType.NameChange => SerializeObject(new UserRoleAuditLogSerialized { Name = userRoleUpdate.Name }),
                UserRoleChangeType.DescriptionChange => SerializeObject(new UserRoleAuditLogSerialized { Description = userRoleUpdate.Description }),
                UserRoleChangeType.EicFunctionChange => SerializeObject(new UserRoleAuditLogSerialized { EicFunction = userRoleUpdate.EicFunction }),
                UserRoleChangeType.StatusChange => SerializeObject(new UserRoleAuditLogSerialized { Status = userRoleUpdate.Status }),
                UserRoleChangeType.PermissionsChange => SerializeObject(new UserRoleAuditLogSerialized { Permissions = userRoleUpdate.Permissions }),
                _ => string.Empty
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

        private static UserRoleAuditLogSerialized MapToUserRoleAuditLogSerialized(UserRoleWithPermissionsDto from)
        {
            return new UserRoleAuditLogSerialized()
            {
                Name = from.Name,
                Description = from.Description,
                EicFunction = from.EicFunction,
                Status = from.Status,
                Permissions = from.Permissions
            };
        }
    }
}
