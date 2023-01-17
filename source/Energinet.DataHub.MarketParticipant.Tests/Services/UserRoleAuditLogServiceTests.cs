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
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoles;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Moq;
using Xunit;

namespace Energinet.DataHub.MarketParticipant.Tests.Services
{
    public class UserRoleAuditLogServiceTests
    {
        [Fact]
        public void DetermineUserRoleChangesAndBuildAuditLogs_CreatedAuditLog()
        {
            // arrange
            var userRoleAuditLogService = new UserRoleAuditLogService();

            var permissions = new[] { "permission1", "permission2" };
            var userRoleDb = BuildUserRoleWithPermissionsDto(name: string.Empty);
            var userRoleUpdateSut = BuildUserRoleWithPermissionsDto(permissions: permissions);

            // act
            var auditLogs = userRoleAuditLogService.DetermineUserRoleChangesAndBuildAuditLogs(
                new UserId(Guid.Empty),
                userRoleDb,
                userRoleUpdateSut).ToList();

            // assert
            var createdAuditLog = auditLogs.First();
            var deserializedCreatedAuditLog = JsonSerializer.Deserialize<UserRoleAuditLogSerialized>(createdAuditLog.ChangeDescriptionJson);
            Assert.True(auditLogs.Count == 1);
            Assert.NotNull(deserializedCreatedAuditLog);
            Assert.NotNull(deserializedCreatedAuditLog.Permissions);
            Assert.Equal(userRoleUpdateSut.Name, deserializedCreatedAuditLog.Name);
            Assert.Equal(userRoleUpdateSut.Description, deserializedCreatedAuditLog.Description);
            Assert.Equal(userRoleUpdateSut.Status, deserializedCreatedAuditLog.Status);
            Assert.Equal(userRoleUpdateSut.EicFunction, deserializedCreatedAuditLog.EicFunction);
            Assert.Equal(userRoleUpdateSut.Permissions, deserializedCreatedAuditLog.Permissions);
        }

        [Fact]
        public void DetermineUserRoleChangesAndBuildAuditLogs_NameChangeAuditLog()
        {
            // arrange
            var userRoleAuditLogService = new UserRoleAuditLogService();

            var userRoleDb = BuildUserRoleWithPermissionsDto(name: "CurrentName");
            var userRoleUpdateSut = BuildUserRoleWithPermissionsDto(name: "NewName");

            // act
            var auditLogs = userRoleAuditLogService.DetermineUserRoleChangesAndBuildAuditLogs(
                new UserId(Guid.Empty),
                userRoleDb,
                userRoleUpdateSut).ToList();

            // assert
            var createdAuditLog = auditLogs.First();
            var deserializedCAuditLog = JsonSerializer.Deserialize<UserRoleAuditLogSerialized>(createdAuditLog.ChangeDescriptionJson);
            Assert.True(auditLogs.Count == 1);
            Assert.NotNull(deserializedCAuditLog);
            Assert.Equal(userRoleUpdateSut.Name, deserializedCAuditLog.Name);
        }

        [Fact]
        public void DetermineUserRoleChangesAndBuildAuditLogs_NameChange_StatusChange_AuditLog()
        {
            // arrange
            var userRoleAuditLogService = new UserRoleAuditLogService();

            var userRoleDb = BuildUserRoleWithPermissionsDto(name: "CurrentName", status: UserRoleStatus.Active);
            var userRoleUpdateSut = BuildUserRoleWithPermissionsDto(name: "NewName", status: UserRoleStatus.Inactive);

            // act
            var auditLogs = userRoleAuditLogService.DetermineUserRoleChangesAndBuildAuditLogs(
                new UserId(Guid.Empty),
                userRoleDb,
                userRoleUpdateSut).ToList();

            // assert
            var deserializedAuditLogs = auditLogs.Select(e => JsonSerializer.Deserialize<UserRoleAuditLogSerialized>(e.ChangeDescriptionJson)).ToList();
            Assert.True(auditLogs.Count == 2);
            Assert.Contains(auditLogs, e => e.UserRoleChangeType == UserRoleChangeType.NameChange);
            Assert.Contains(auditLogs, e => e.UserRoleChangeType == UserRoleChangeType.StatusChange);
            Assert.Contains(deserializedAuditLogs, e => e?.Status != null);
            Assert.Contains(deserializedAuditLogs, e => !string.IsNullOrWhiteSpace(e?.Name));
            Assert.True(deserializedAuditLogs.All(e => e?.EicFunction == null));
        }

        [Fact]
        public void DetermineUserRoleChangesAndBuildAuditLogs_UserIdNull_Throws()
        {
            // arrange
            DetermineUserRoleChangesAndBuildAuditLogs_AssertThrows(null!, BuildUserRoleWithPermissionsDto(), BuildUserRoleWithPermissionsDto());
        }

        [Fact]
        public void DetermineUserRoleChangesAndBuildAuditLogs_userRoleDbNull_Throws()
        {
            // arrange
            DetermineUserRoleChangesAndBuildAuditLogs_AssertThrows(new UserId(Guid.Empty), null!, BuildUserRoleWithPermissionsDto());
        }

        [Fact]
        public void DetermineUserRoleChangesAndBuildAuditLogs_userRoleUpdateNull_Throws()
        {
            // arrange
            DetermineUserRoleChangesAndBuildAuditLogs_AssertThrows(new UserId(Guid.Empty), BuildUserRoleWithPermissionsDto(), null!);
        }

        private static void DetermineUserRoleChangesAndBuildAuditLogs_AssertThrows(
            UserId currentUserId,
            UserRoleWithPermissionsDto userRoleDb,
            UserRoleWithPermissionsDto userRoleUpdate)
        {
            // arrange
            var userRoleAuditLogService = new UserRoleAuditLogService();

            // act + assert
            Assert.Throws<ArgumentNullException>(() =>
                userRoleAuditLogService.DetermineUserRoleChangesAndBuildAuditLogs(
                    currentUserId,
                    userRoleDb,
                    userRoleUpdate));
        }

        private static UserRoleWithPermissionsDto BuildUserRoleWithPermissionsDto(
            string name = "UserRoleName",
            string description = "UserRoleDescription",
            EicFunction eicFunction = EicFunction.Agent,
            UserRoleStatus status = UserRoleStatus.Active,
            IEnumerable<string>? permissions = null)
        {
            return new UserRoleWithPermissionsDto(
                Guid.NewGuid(),
                name,
                description,
                eicFunction,
                status,
                permissions ?? Enumerable.Empty<string>());
        }
    }
}
