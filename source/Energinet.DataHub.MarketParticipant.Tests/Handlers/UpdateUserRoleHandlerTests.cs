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
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoles;
using Energinet.DataHub.MarketParticipant.Application.Handlers.UserRoles;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
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
    public sealed class UpdateUserRoleHandlerTests
    {
        private const int ValidPermission = (int)PermissionId.ActorsManage;

        [Fact]
        public async Task Handle_UpdateUserRole_UserRoleNotFound()
        {
            // Arrange
            var userRoleRepositoryMock = new Mock<IUserRoleRepository>();
            var userRoleAuditLogServiceMock = new Mock<IUserRoleAuditLogService>();
            var userRoleAuditLogEntryRepositoryMock = new Mock<IUserRoleAuditLogEntryRepository>();
            var target = new UpdateUserRoleHandler(userRoleRepositoryMock.Object, userRoleAuditLogServiceMock.Object, userRoleAuditLogEntryRepositoryMock.Object);
            var roleId = Guid.NewGuid();
            userRoleRepositoryMock
                .Setup(x => x.GetAsync(It.IsAny<UserRoleId>()))
                .ReturnsAsync((UserRole?)null);

            var updateUserRoleCommand = new UpdateUserRoleCommand(Guid.NewGuid(), roleId, new UpdateUserRoleDto(
                "newName",
                "newDescription",
                UserRoleStatus.Active,
                new Collection<int> { ValidPermission }));

            // Act + Assert
            await Assert.ThrowsAsync<NotFoundValidationException>(() =>
                target.Handle(updateUserRoleCommand, CancellationToken.None)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Handle_UpdateUserRole_UserRoleWithSameNameExists()
        {
            // Arrange
            var userRoleRepositoryMock = new Mock<IUserRoleRepository>();
            var userRoleAuditLogServiceMock = new Mock<IUserRoleAuditLogService>();
            var userRoleAuditLogEntryRepositoryMock = new Mock<IUserRoleAuditLogEntryRepository>();
            var target = new UpdateUserRoleHandler(userRoleRepositoryMock.Object, userRoleAuditLogServiceMock.Object, userRoleAuditLogEntryRepositoryMock.Object);

            var existingUserRoleWithSameName = new UserRole(
                new UserRoleId(Guid.NewGuid()),
                "UserRoleNameNew",
                "fake_value",
                UserRoleStatus.Active,
                new List<PermissionId>(),
                EicFunction.BillingAgent);

            userRoleRepositoryMock
                .Setup(x => x.GetByNameInMarketRoleAsync(It.IsAny<string>(), existingUserRoleWithSameName.EicFunction))
                .ReturnsAsync(existingUserRoleWithSameName);

            var userRoleToUpdate = new UserRole(
                new UserRoleId(Guid.NewGuid()),
                "UserRoleName",
                "fake_value",
                UserRoleStatus.Active,
                new List<PermissionId>(),
                EicFunction.BillingAgent);

            var updateUserRoleCommand = new UpdateUserRoleCommand(Guid.NewGuid(), userRoleToUpdate.Id.Value, new UpdateUserRoleDto(
                "UserRoleNameNew",
                "fake_value",
                UserRoleStatus.Active,
                new Collection<int> { ValidPermission }));

            userRoleRepositoryMock
                .Setup(x => x.GetAsync(It.IsAny<UserRoleId>()))
                .ReturnsAsync(userRoleToUpdate);

            // Act + Assert
            await Assert.ThrowsAsync<ValidationException>(() =>
                target.Handle(updateUserRoleCommand, CancellationToken.None)).ConfigureAwait(false);
        }
    }
}
