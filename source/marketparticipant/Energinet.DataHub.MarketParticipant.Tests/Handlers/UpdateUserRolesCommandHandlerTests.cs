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
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoles;
using Energinet.DataHub.MarketParticipant.Application.Handlers.UserRoles;
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
        public async Task Handle_UserRoleAssignment_AddDeactivatedRole_ThrowsException()
        {
            // Arrange
            var userRepositoryMock = new Mock<IUserRepository>();
            var userRoleRepositoryMock = new Mock<IUserRoleRepository>();
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
                EicFunction.BillingAgent);

            userRepositoryMock.Setup(x => x.GetAsync(user.Id)).ReturnsAsync(user);
            userRoleRepositoryMock.Setup(x => x.GetAsync(deactivatedUserRoleId)).ReturnsAsync(deactivatedUserRole);

            var target = new UpdateUserRolesHandler(
                userRepositoryMock.Object,
                userRoleRepositoryMock.Object);

            var updatedRoleAssignments = new List<Guid> { deactivatedUserRoleId.Value, Guid.NewGuid() };

            var command = new UpdateUserRoleAssignmentsCommand(
                actorId,
                userId,
                new UpdateUserRoleAssignmentsDto(updatedRoleAssignments, new[] { userRoleAssignments[1].UserRoleId.Value }));

            // Act + Assert
            var ex = await Assert.ThrowsAsync<ValidationException>(() =>
                target.Handle(command, CancellationToken.None));
            Assert.Contains("is not in active status and can't be added as a role", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Handle_UserRoleAssignment_AddUnknownStatusRole_ThrowsException()
        {
            // Arrange
            var userRepositoryMock = new Mock<IUserRepository>();
            var userRoleRepositoryMock = new Mock<IUserRoleRepository>();
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
                EicFunction.BillingAgent);

            userRepositoryMock.Setup(x => x.GetAsync(user.Id)).ReturnsAsync(user);
            userRoleRepositoryMock.Setup(x => x.GetAsync(deactivatedUserRoleId)).ReturnsAsync(deactivatedUserRole);

            var target = new UpdateUserRolesHandler(
                userRepositoryMock.Object,
                userRoleRepositoryMock.Object);

            var updatedRoleAssignments = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

            var command = new UpdateUserRoleAssignmentsCommand(
                actorId,
                userId,
                new UpdateUserRoleAssignmentsDto(updatedRoleAssignments, new[] { userRoleAssignments[1].UserRoleId.Value }));

            // Act + Assert
            var ex = await Assert.ThrowsAsync<ValidationException>(() =>
                target.Handle(command, CancellationToken.None));
            Assert.Contains("does not exist and can't be added as a role", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
    }
}
