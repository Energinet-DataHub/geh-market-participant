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
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.Core.App.Common;
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoles;
using Energinet.DataHub.MarketParticipant.Application.Handlers.UserRoles;
using Energinet.DataHub.MarketParticipant.Application.Security;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Handlers
{
    [UnitTest]
    public sealed class UpdateUserRolesCommandHandlerTests
    {
        [Fact]
        public async Task Handle_UserRoleAssignment_TwoAuditLogsAdded_OneAuditLogsRemoved()
        {
            // Arrange
            var userContextMock = CreateMockedUser();
            var userRepositoryMock = new Mock<IUserRepository>();
            var auditLogEntryRepository = new Mock<IUserRoleAssignmentAuditLogEntryRepository>();
            var userRoleRepositoryMock = new Mock<IUserRoleRepository>();
            var externalUserId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var actorId = Guid.NewGuid();
            var addedRole1Id = new UserRoleId("d1c79f70-ee7f-4fea-8fa9-c06febdbebc8");
            var addedRole2Id = new UserRoleId("6ce873ac-2c49-49fa-8b6f-338d080f6390");
            var userRoleAssignments = new List<UserRoleAssignment>
            {
                new(new ActorId(Guid.NewGuid()), new UserRoleId(Guid.NewGuid())),
                new(new ActorId(actorId), new UserRoleId(Guid.NewGuid()))
            };
            var user = new User(
                new UserId(userId),
                new ActorId(actorId),
                new ExternalUserId(externalUserId),
                userRoleAssignments,
                null,
                null);

            var role1 = new UserRole(
                addedRole1Id,
                "test",
                "test",
                UserRoleStatus.Active,
                new List<PermissionId>(),
                EicFunction.BillingAgent,
                userContextMock.CurrentUser.UserId);

            var role2 = new UserRole(
                addedRole2Id,
                "test",
                "test",
                UserRoleStatus.Active,
                new List<PermissionId>(),
                EicFunction.BillingAgent,
                userContextMock.CurrentUser.UserId);

            userRepositoryMock.Setup(x => x.GetAsync(user.Id)).ReturnsAsync(user);
            userRoleRepositoryMock.Setup(x => x
                .GetAsync(It.IsAny<UserRoleId>()))
                .ReturnsAsync<UserRoleId, IUserRoleRepository, UserRole?>((role) =>
                {
                    return role.Value.ToString() switch
                    {
                        "d1c79f70-ee7f-4fea-8fa9-c06febdbebc8" => role1,
                        "6ce873ac-2c49-49fa-8b6f-338d080f6390" => role2,
                        _ => null
                    };
                });

            var target = new UpdateUserRolesHandler(
                userRepositoryMock.Object,
                auditLogEntryRepository.Object,
                userContextMock,
                userRoleRepositoryMock.Object);

            var updatedRoleAssignments = new List<Guid> { addedRole1Id.Value, addedRole2Id.Value };

            var command = new UpdateUserRoleAssignmentsCommand(
                actorId,
                userId,
                new UpdateUserRoleAssignmentsDto(updatedRoleAssignments, new[] { userRoleAssignments[1].UserRoleId.Value }));

            // act
            await target.Handle(command, CancellationToken.None);

            // assert
            auditLogEntryRepository.Verify(
                x => x.InsertAuditLogEntryAsync(
                    user.Id,
                    It.Is<UserRoleAssignmentAuditLogEntry>(a => a.AssignmentType == UserRoleAssignmentTypeAuditLog.Added)),
                Times.Exactly(2));

            auditLogEntryRepository.Verify(
                x => x.InsertAuditLogEntryAsync(
                    user.Id,
                    It.Is<UserRoleAssignmentAuditLogEntry>(a => a.AssignmentType == UserRoleAssignmentTypeAuditLog.Removed)),
                Times.Exactly(1));
        }

        [Fact]
        public async Task Handle_UserRoleAssignment_AddDeactivatedRole_ThrowsException()
        {
            // Arrange
            var userContextMock = CreateMockedUser();
            var userRepositoryMock = new Mock<IUserRepository>();
            var userRoleRepositoryMock = new Mock<IUserRoleRepository>();
            var auditLogEntryRepository = new Mock<IUserRoleAssignmentAuditLogEntryRepository>();
            var externalUserId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var actorId = Guid.NewGuid();
            var deactivatedUserRoleId = new UserRoleId(Guid.NewGuid());
            var userRoleAssignments = new List<UserRoleAssignment>
            {
                new(new ActorId(Guid.NewGuid()), new UserRoleId(Guid.NewGuid())),
                new(new ActorId(actorId), new UserRoleId(Guid.NewGuid()))
            };
            var user = new User(
                new UserId(userId),
                new ActorId(actorId),
                new ExternalUserId(externalUserId),
                userRoleAssignments,
                null,
                null);

            var deactivatedUserRole = new UserRole(
                deactivatedUserRoleId,
                "test",
                "test",
                UserRoleStatus.Inactive,
                new List<PermissionId>(),
                EicFunction.BillingAgent,
                userContextMock.CurrentUser.UserId);

            userRepositoryMock.Setup(x => x.GetAsync(user.Id)).ReturnsAsync(user);
            userRoleRepositoryMock.Setup(x => x.GetAsync(deactivatedUserRoleId)).ReturnsAsync(deactivatedUserRole);

            var target = new UpdateUserRolesHandler(
                userRepositoryMock.Object,
                auditLogEntryRepository.Object,
                userContextMock,
                userRoleRepositoryMock.Object);

            var updatedRoleAssignments = new List<Guid> { deactivatedUserRoleId.Value, Guid.NewGuid() };

            var command = new UpdateUserRoleAssignmentsCommand(
                actorId,
                userId,
                new UpdateUserRoleAssignmentsDto(updatedRoleAssignments, new[] { userRoleAssignments[1].UserRoleId.Value }));

            // Act + Assert
            var ex = await Assert.ThrowsAsync<ValidationException>(() =>
                target.Handle(command, CancellationToken.None)).ConfigureAwait(false);
            Assert.Contains("is not in active status and can't be added as a role", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Handle_UserRoleAssignment_AddUnkownStatusRole_ThrowsException()
        {
            // Arrange
            var userContextMock = CreateMockedUser();
            var userRepositoryMock = new Mock<IUserRepository>();
            var userRoleRepositoryMock = new Mock<IUserRoleRepository>();
            var auditLogEntryRepository = new Mock<IUserRoleAssignmentAuditLogEntryRepository>();
            var externalUserId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var actorId = Guid.NewGuid();
            var deactivatedUserRoleId = new UserRoleId(Guid.NewGuid());
            var userRoleAssignments = new List<UserRoleAssignment>
            {
                new(new ActorId(Guid.NewGuid()), new UserRoleId(Guid.NewGuid())),
                new(new ActorId(actorId), new UserRoleId(Guid.NewGuid()))
            };
            var user = new User(
                new UserId(userId),
                new ActorId(actorId),
                new ExternalUserId(externalUserId),
                userRoleAssignments,
                null,
                null);

            var deactivatedUserRole = new UserRole(
                deactivatedUserRoleId,
                "test",
                "test",
                (UserRoleStatus)255,
                new List<PermissionId>(),
                EicFunction.BillingAgent,
                userContextMock.CurrentUser.UserId);

            userRepositoryMock.Setup(x => x.GetAsync(user.Id)).ReturnsAsync(user);
            userRoleRepositoryMock.Setup(x => x.GetAsync(deactivatedUserRoleId)).ReturnsAsync(deactivatedUserRole);

            var target = new UpdateUserRolesHandler(
                userRepositoryMock.Object,
                auditLogEntryRepository.Object,
                userContextMock,
                userRoleRepositoryMock.Object);

            var updatedRoleAssignments = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

            var command = new UpdateUserRoleAssignmentsCommand(
                actorId,
                userId,
                new UpdateUserRoleAssignmentsDto(updatedRoleAssignments, new[] { userRoleAssignments[1].UserRoleId.Value }));

            // Act + Assert
            var ex = await Assert.ThrowsAsync<ValidationException>(() =>
                target.Handle(command, CancellationToken.None)).ConfigureAwait(false);
            Assert.Contains("does not exist and can't be added as a role", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        private static UserContext<FrontendUser> CreateMockedUser()
        {
            var frontendUser = new FrontendUser(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                false);

            var userContext = new UserContext<FrontendUser>();
            userContext.SetCurrentUser(frontendUser);
            return userContext;
        }
    }
}
