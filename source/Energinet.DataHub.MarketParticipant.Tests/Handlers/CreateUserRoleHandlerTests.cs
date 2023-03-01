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
using Energinet.DataHub.Core.App.Common.Security;
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoles;
using Energinet.DataHub.MarketParticipant.Application.Handlers.UserRoles;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Handlers
{
    [UnitTest]
    public sealed class CreateUserRoleHandlerTests
    {
        private const int ValidPermission = (int)Permission.ActorManage;

        [Fact]
        public async Task Handle_NewUserRole_UserRoleIdReturned()
        {
            // Arrange
            var userRoleRepositoryMock = new Mock<IUserRoleRepository>();
            var userRoleAuditLogServiceMock = new Mock<IUserRoleAuditLogService>();
            var userRoleAuditLogEntryRepositoryMock = new Mock<IUserRoleAuditLogEntryRepository>();
            var userRoleHelperServiceMock = new Mock<IEnsureUserRolePermissionsService>();
            var target = new CreateUserRoleHandler(
                userRoleRepositoryMock.Object,
                userRoleAuditLogServiceMock.Object,
                userRoleAuditLogEntryRepositoryMock.Object,
                userRoleHelperServiceMock.Object);

            var roleId = Guid.NewGuid();
            var userRole = new UserRole(
                new UserRoleId(roleId),
                "fake_value",
                "fake_value",
                UserRoleStatus.Active,
                new List<Permission>(),
                EicFunction.BillingAgent);

            userRoleRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<UserRole>()))
                .ReturnsAsync(userRole.Id);

            userRoleHelperServiceMock
                .Setup(x => x.EnsurePermissionsSelectedAreValidForMarketRoleAsync(It.IsAny<IEnumerable<Permission>>(), It.IsAny<EicFunction>()))
                .ReturnsAsync(true);

            var command = new CreateUserRoleCommand(userRole.Id.Value, new CreateUserRoleDto(
                "fake_value",
                "fake_value",
                UserRoleStatus.Active,
                EicFunction.BillingAgent,
                new Collection<int> { ValidPermission }));

            // Act
            var response = await target
                .Handle(command, CancellationToken.None)
                .ConfigureAwait(false);

            // Assert
            Assert.Equal(userRole.Id.Value, response.UserRoleId);
        }

        [Fact]
        public async Task Handle_InvalidPermissions_ThrowsException()
        {
            // Arrange
            var userRoleRepositoryMock = new Mock<IUserRoleRepository>();
            var userRoleAuditLogServiceMock = new Mock<IUserRoleAuditLogService>();
            var userRoleAuditLogEntryRepositoryMock = new Mock<IUserRoleAuditLogEntryRepository>();
            var userRoleHelperServiceMock = new Mock<IEnsureUserRolePermissionsService>();
            var target = new CreateUserRoleHandler(
                userRoleRepositoryMock.Object,
                userRoleAuditLogServiceMock.Object,
                userRoleAuditLogEntryRepositoryMock.Object,
                userRoleHelperServiceMock.Object);

            var roleId = Guid.NewGuid();
            var userRole = new UserRole(
                new UserRoleId(roleId),
                "fake_value",
                "fake_value",
                UserRoleStatus.Active,
                new List<Permission>(),
                EicFunction.BillingAgent);

            userRoleRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<UserRole>()))
                .ReturnsAsync(userRole.Id);

            userRoleHelperServiceMock
                .Setup(x => x.EnsurePermissionsSelectedAreValidForMarketRoleAsync(It.IsAny<IEnumerable<Permission>>(), It.IsAny<EicFunction>()))
                .ReturnsAsync(false);

            var command = new CreateUserRoleCommand(userRole.Id.Value, new CreateUserRoleDto(
                "fake_value",
                "fake_value",
                UserRoleStatus.Active,
                EicFunction.BillingAgent,
                new Collection<int> { ValidPermission }));

            // Act + Assert
            await Assert
                .ThrowsAsync<ValidationException>(() => target.Handle(command, CancellationToken.None))
                .ConfigureAwait(false);
        }
    }
}
