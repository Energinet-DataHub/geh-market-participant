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
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoleTemplates;
using Energinet.DataHub.MarketParticipant.Application.Handlers.UserRoleTemplates;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Handlers
{
    [UnitTest]
    public sealed class UpdateUserRoleTemplatesCommandHandlerTests
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
                new(new UserId(userId), Guid.NewGuid(), new UserRoleTemplateId(Guid.NewGuid())),
                new(new UserId(userId), actorId, new UserRoleTemplateId(Guid.NewGuid())),
            };
            var user = new User(
                new UserId(userId),
                new ExternalUserId(externalUserId),
                userRoleAssignments,
                new EmailAddress("test@test.dk"));

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetAsync(new UserId(userId)))
                .ReturnsAsync(user);

            var auditLogEntryRepository = new Mock<IUserRoleAssignmentAuditLogEntryRepository>();

            var target = new UpdateUserRoleTemplatesCommandHandler(
                userRepositoryMock.Object,
                auditLogEntryRepository.Object);

            var updatedRoleAssignments = new List<UserRoleTemplateId>() { new(Guid.NewGuid()), new(Guid.NewGuid()) };

            var command = new UpdateUserRoleAssignmentsCommand(
                userId,
                new UpdateUserRoleAssignmentsDto(actorId, updatedRoleAssignments));

            // act
            await target.Handle(command, CancellationToken.None);

            // assert
            auditLogEntryRepository.Verify(
                x => x.InsertAuditLogEntryAsync(
                    It.Is<UserRoleAssignmentAuditLogEntry>(a => a.AssignmentType == UserRoleAssignmentTypeAuditLog.Added)),
                Times.Exactly(2));

            auditLogEntryRepository.Verify(
                x => x.InsertAuditLogEntryAsync(
                    It.Is<UserRoleAssignmentAuditLogEntry>(a => a.AssignmentType == UserRoleAssignmentTypeAuditLog.Removed)),
                Times.Exactly(1));
        }

        [Fact]
        public async Task Handle_UserRoleAssignment_NoChanges()
        {
            // arrange
            var externalUserId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var actorId = Guid.NewGuid();
            var userRoleId1 = Guid.NewGuid();
            var userRoleId2 = Guid.NewGuid();

            var userRoleAssignments = new List<UserRoleAssignment>
            {
                new(new UserId(userId), Guid.NewGuid(), new UserRoleTemplateId(Guid.NewGuid())),
                new(new UserId(userId), actorId, new UserRoleTemplateId(userRoleId1)),
                new(new UserId(userId), actorId, new UserRoleTemplateId(userRoleId2))
            };
            var user = new User(
                new UserId(userId),
                new ExternalUserId(externalUserId),
                userRoleAssignments,
                new EmailAddress("test@test.dk"));

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetAsync(new UserId(userId)))
                .ReturnsAsync(user);

            var auditLogEntryRepository = new Mock<IUserRoleAssignmentAuditLogEntryRepository>();

            var target = new UpdateUserRoleTemplatesCommandHandler(
                userRepositoryMock.Object,
                auditLogEntryRepository.Object);

            var updatedRoleAssignments = new List<UserRoleTemplateId>() { new(userRoleId1), new(userRoleId2) };

            var command = new UpdateUserRoleAssignmentsCommand(
                userId,
                new UpdateUserRoleAssignmentsDto(actorId, updatedRoleAssignments));

            // act
            await target.Handle(command, CancellationToken.None);

            // assert
            auditLogEntryRepository.Verify(
                x => x.InsertAuditLogEntryAsync(
                    It.Is<UserRoleAssignmentAuditLogEntry>(a => a.AssignmentType == UserRoleAssignmentTypeAuditLog.Added)),
                Times.Never);

            auditLogEntryRepository.Verify(
                x => x.InsertAuditLogEntryAsync(
                    It.Is<UserRoleAssignmentAuditLogEntry>(a => a.AssignmentType == UserRoleAssignmentTypeAuditLog.Removed)),
                Times.Never);
        }
    }
}
