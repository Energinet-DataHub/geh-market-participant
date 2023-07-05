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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.Core.App.Common.Abstractions.Users;
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoles;
using Energinet.DataHub.MarketParticipant.Application.Handlers.UserRoles;
using Energinet.DataHub.MarketParticipant.Application.Security;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Tests.Common;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Handlers
{
    [UnitTest]
    public sealed class DeactivateUserRoleHandlerTests
    {
        [Fact]
        public async Task HandleCommand_NoUserRole_ThrowsNotFoundException()
        {
            // Arrange
            var userRoleRepositoryMock = new Mock<IUserRoleRepository>();
            var userRepository = new Mock<IUserRepository>();
            var target = new DeactivateUserRoleHandler(
                userRepository.Object,
                new Mock<IUserRoleAssignmentAuditLogEntryRepository>().Object,
                new Mock<IUserContext<FrontendUser>>().Object,
                userRoleRepositoryMock.Object,
                UnitOfWorkProviderMock.Create(),
                new Mock<IUserRoleAuditLogService>().Object,
                new Mock<IUserRoleAuditLogEntryRepository>().Object);

            var userRoleId = Guid.NewGuid();
            var changedByUserId = Guid.NewGuid();

            userRoleRepositoryMock
                .Setup(mockRepo => mockRepo.GetAsync(It.IsAny<UserRoleId>()))
                .ReturnsAsync((UserRole?)null);

            var command = new DeactivateUserRoleCommand(userRoleId, changedByUserId);

            // Act + Assert
            await Assert
                .ThrowsAsync<NotFoundValidationException>(() => target.Handle(command, CancellationToken.None))
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task HandleCommand_UpdatesStatusOnRoleAsync()
        {
            // arrange
            var userRoleRepositoryMock = new Mock<IUserRoleRepository>();
            var userRepositoryMock = new Mock<IUserRepository>();
            var userContextMock = new Mock<IUserContext<FrontendUser>>();
            var userAndRole = MockUserRoleWithUserAssigned(
                userRoleRepositoryMock,
                userRepositoryMock,
                userContextMock,
                Guid.NewGuid(),
                Guid.NewGuid());

            var target = new DeactivateUserRoleHandler(
                userRepositoryMock.Object,
                new Mock<IUserRoleAssignmentAuditLogEntryRepository>().Object,
                userContextMock.Object,
                userRoleRepositoryMock.Object,
                UnitOfWorkProviderMock.Create(),
                new Mock<IUserRoleAuditLogService>().Object,
                new Mock<IUserRoleAuditLogEntryRepository>().Object);

            var changedByUserId = Guid.NewGuid();
            var command = new DeactivateUserRoleCommand(userAndRole.UserRole.Id.Value, changedByUserId);

            // Act
            await target.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(UserRoleStatus.Inactive, userAndRole.UserRole.Status);
        }

        [Fact]
        public async Task HandleCommand_RemovesRoleFromUserAsync()
        {
            // arrange
            var userRoleRepositoryMock = new Mock<IUserRoleRepository>();
            var userRepositoryMock = new Mock<IUserRepository>();
            var userContextMock = new Mock<IUserContext<FrontendUser>>();
            var userAndRole = MockUserRoleWithUserAssigned(
                userRoleRepositoryMock,
                userRepositoryMock,
                userContextMock,
                Guid.NewGuid(),
                Guid.NewGuid());

            var numberOfRolesBeforeCommand = userAndRole.User.RoleAssignments.Count;
            var target = new DeactivateUserRoleHandler(
                userRepositoryMock.Object,
                new Mock<IUserRoleAssignmentAuditLogEntryRepository>().Object,
                userContextMock.Object,
                userRoleRepositoryMock.Object,
                UnitOfWorkProviderMock.Create(),
                new Mock<IUserRoleAuditLogService>().Object,
                new Mock<IUserRoleAuditLogEntryRepository>().Object);

            var changedByUserId = Guid.NewGuid();
            var command = new DeactivateUserRoleCommand(userAndRole.UserRole.Id.Value, changedByUserId);

            // Act
            await target.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(1, numberOfRolesBeforeCommand);
            Assert.Empty(userAndRole.User.RoleAssignments);
        }

        private static (UserRole UserRole, User User) MockUserRoleWithUserAssigned(
            Mock<IUserRoleRepository> userRoleRepositoryMock,
            Mock<IUserRepository> userRepositoryMock,
            Mock<IUserContext<FrontendUser>> userContextMock,
            Guid userRoleId,
            Guid userId)
        {
            var actor = TestPreparationModels.MockedActor();
            var userRole = TestPreparationModels.MockedUserRole(userRoleId);
            var user = TestPreparationModels.MockedUser(userId);
            user.RoleAssignments.Add(new UserRoleAssignment(actor.Id, userRole.Id));

            userRoleRepositoryMock
                .Setup(userRoleRepository => userRoleRepository.GetAsync(new UserRoleId(userRoleId)))
                .ReturnsAsync(userRole);
            userRepositoryMock
                .Setup(userRepository => userRepository.GetToUserRoleAsync(userRole.Id))
                .ReturnsAsync(new List<User>() { user });
            userContextMock
                .Setup(context => context.CurrentUser)
                .Returns(new FrontendUser(userId, Guid.NewGuid(), actor.Id.Value, false));

            return (userRole, user);
        }
    }
}
