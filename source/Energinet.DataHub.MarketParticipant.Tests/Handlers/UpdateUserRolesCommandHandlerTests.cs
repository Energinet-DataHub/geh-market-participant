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
using Energinet.DataHub.Core.App.Common;
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoles;
using Energinet.DataHub.MarketParticipant.Application.Handlers.UserRoles;
using Energinet.DataHub.MarketParticipant.Application.Security;
using Energinet.DataHub.MarketParticipant.Domain.Model;
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
            // arrange
            var externalUserId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var actorId = Guid.NewGuid();
            var userRoleAssignments = new List<UserRoleAssignment>()
            {
                new(Guid.NewGuid(), new UserRoleId(Guid.NewGuid())),
                new(actorId, new UserRoleId(Guid.NewGuid())),
            };
            var user = new User(
                new UserId(userId),
                new ExternalUserId(externalUserId),
                userRoleAssignments,
                new EmailAddress("test@test.dk"));

            var userContextMock = CreateMockedUser();
            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetAsync(user.Id)).ReturnsAsync(user);
            userRepositoryMock.Setup(x => x.GetAsync(new ExternalUserId(userContextMock.CurrentUser.ExternalUserId)))
                .ReturnsAsync(new User(
                    new UserId(Guid.NewGuid()),
                    new ExternalUserId(userContextMock.CurrentUser.ExternalUserId),
                    new List<UserRoleAssignment>(),
                    new EmailAddress("fake@value")));

            var auditLogEntryRepository = new Mock<IUserRoleAssignmentAuditLogEntryRepository>();

            var target = new UpdateUserRolesHandler(
                userRepositoryMock.Object,
                auditLogEntryRepository.Object,
                userContextMock);

            var updatedRoleAssignments = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

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
