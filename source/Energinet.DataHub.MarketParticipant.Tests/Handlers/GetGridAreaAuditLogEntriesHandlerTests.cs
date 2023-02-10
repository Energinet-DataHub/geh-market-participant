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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.GridArea;
using Energinet.DataHub.MarketParticipant.Application.Handlers.GridArea;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users.Authentication;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Handlers
{
    [UnitTest]
    public sealed class GetGridAreaAuditLogEntriesHandlerTests
    {
        [Fact]
        public async Task Handle_NullArgument_ThrowsException()
        {
            // arrange
            var target = new GetGridAreaAuditLogEntriesHandler(
                new Mock<IGridAreaAuditLogEntryRepository>().Object,
                new Mock<IUserRepository>().Object,
                new Mock<IUserIdentityRepository>().Object);

            // act assert
            await Assert
                .ThrowsAsync<ArgumentNullException>(() => target.Handle(null!, CancellationToken.None))
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task Handle_Command_CallsRepository()
        {
            // arrange
            var userId = new UserId(Guid.NewGuid());
            var externalUserId = new ExternalUserId(Guid.NewGuid());

            var repositoryMock = new Mock<IGridAreaAuditLogEntryRepository>();
            repositoryMock
                .Setup(x => x.GetAsync(It.IsAny<GridAreaId>()))
                .ReturnsAsync(new[]
                    {
                        new GridAreaAuditLogEntry(DateTimeOffset.UtcNow, userId, Domain.Model.GridAreaAuditLogEntryField.Name, "oldVal", "newVal", new GridAreaId(Guid.NewGuid()))
                    });

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock
                .Setup(userRepository => userRepository.GetAsync(userId))
                .ReturnsAsync(new User(userId, externalUserId, Enumerable.Empty<UserRoleAssignment>()));

            var userIdentityRepositoryMock = new Mock<IUserIdentityRepository>();
            userIdentityRepositoryMock
                .Setup(x => x.GetAsync(externalUserId))
                .ReturnsAsync(new UserIdentity(
                    externalUserId,
                    new EmailAddress("fake@value"),
                    UserStatus.Active,
                    "first",
                    "last",
                    null,
                    DateTimeOffset.UtcNow,
                    AuthenticationMethod.Undetermined));

            var target = new GetGridAreaAuditLogEntriesHandler(
                repositoryMock.Object,
                userRepositoryMock.Object,
                userIdentityRepositoryMock.Object);

            // act
            var actual = await target.Handle(new GetGridAreaAuditLogEntriesCommand(Guid.NewGuid()), CancellationToken.None).ConfigureAwait(false);

            // assert
            Assert.Single(actual.GridAreaAuditLogEntries);
            Assert.Equal("first last", actual.GridAreaAuditLogEntries.Single().UserDisplayName);
        }
    }
}
