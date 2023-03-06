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
using Energinet.DataHub.Core.App.Common.Security;
using Energinet.DataHub.MarketParticipant.Application.Commands.Permissions;
using Energinet.DataHub.MarketParticipant.Application.Handlers.Permissions;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Handlers
{
    [UnitTest]
    public sealed class GetPermissionAuditLogEntriesHandlerTests
    {
        [Fact]
        public async Task Handle_Command_IsNull_Throws()
        {
            // arrange
            var repositoryMock = new Mock<IPermissionAuditLogEntryRepository>();

            var target = new GetPermissionAuditLogsHandler(repositoryMock.Object);

            // act + assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => target.Handle(null!, CancellationToken.None)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Handle_Command_CallsRepository()
        {
            // arrange
            var userId = new UserId(Guid.NewGuid());

            var repositoryMock = new Mock<IPermissionAuditLogEntryRepository>();
            repositoryMock
                .Setup(x => x.GetAsync(Permission.UsersView))
                .ReturnsAsync(new[]
                    {
                        new PermissionAuditLogEntry(
                            Permission.UsersView,
                            userId,
                            PermissionChangeType.DescriptionChange,
                            DateTimeOffset.UtcNow),
                        new PermissionAuditLogEntry(
                            Permission.UsersView,
                            userId,
                            PermissionChangeType.DescriptionChange,
                            DateTimeOffset.UtcNow.AddHours(1))
                    });

            var target = new GetPermissionAuditLogsHandler(repositoryMock.Object);

            var command = new GetPermissionAuditLogsCommand((int)Permission.UsersView);

            // act
            var actual = await target.Handle(command, CancellationToken.None).ConfigureAwait(false);

            // assert
            var logs = actual.PermissionAuditLogs.ToList();
            Assert.Equal(2, logs.Count);
            Assert.Equal(2, logs.Count(l => l.PermissionChangeType == (int)PermissionChangeType.DescriptionChange));
            Assert.Equal(2, logs.Count(l => l.ChangedByUserId == userId.Value));
        }
    }
}
