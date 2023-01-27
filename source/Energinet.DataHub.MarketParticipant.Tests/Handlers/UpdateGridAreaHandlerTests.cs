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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.Core.App.Common;
using Energinet.DataHub.MarketParticipant.Application.Commands.GridArea;
using Energinet.DataHub.MarketParticipant.Application.Handlers.GridArea;
using Energinet.DataHub.MarketParticipant.Application.Security;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Moq;
using Xunit;
using Xunit.Categories;
using GridAreaAuditLogEntryField = Energinet.DataHub.MarketParticipant.Domain.Model.GridAreaAuditLogEntryField;

namespace Energinet.DataHub.MarketParticipant.Tests.Handlers
{
    [UnitTest]
    public sealed class UpdateGridAreaHandlerTests
    {
        [Fact]
        public async Task Handle_NullArgument_ThrowsException()
        {
            // arrange
            var target = new UpdateGridAreaHandler(
                new Mock<IGridAreaRepository>().Object,
                new Mock<IGridAreaIntegrationEventsQueueService>().Object,
                UnitOfWorkProviderMock.Create(),
                new Mock<IGridAreaAuditLogEntryRepository>().Object,
                CreateMockedUser());

            // act + Assert
            await Assert
                .ThrowsAsync<ArgumentNullException>(() => target.Handle(null!, CancellationToken.None))
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task Handle_ChangesName()
        {
            // arrange
            var gridAreaId = Guid.NewGuid();
            var gridArea = new GridArea(
                new GridAreaName("name"),
                new GridAreaCode("101"),
                PriceAreaCode.Dk1,
                DateTimeOffset.MinValue,
                null);

            var gridAreaRepository = new Mock<IGridAreaRepository>();
            gridAreaRepository.Setup(x => x.GetAsync(new GridAreaId(gridAreaId)))
                .ReturnsAsync(gridArea);

            var target = new UpdateGridAreaHandler(
                gridAreaRepository.Object,
                new Mock<IGridAreaIntegrationEventsQueueService>().Object,
                UnitOfWorkProviderMock.Create(),
                new Mock<IGridAreaAuditLogEntryRepository>().Object,
                CreateMockedUser());

            // act
            await target.Handle(new UpdateGridAreaCommand(gridAreaId, new ChangeGridAreaDto(gridAreaId, "newName")), CancellationToken.None);

            // assert
            gridAreaRepository.Verify(x => x.AddOrUpdateAsync(It.Is<GridArea>(y => y.Name.Value == "newName")), Times.Once);
        }

        [Fact]
        public async Task Handle_NameChanged_SendsDomainEvent()
        {
            // arrange
            var gridAreaId = Guid.NewGuid();
            var gridArea = new GridArea(
                new GridAreaName("name"),
                new GridAreaCode("101"),
                PriceAreaCode.Dk1,
                DateTimeOffset.MinValue,
                null);

            var gridAreaRepository = new Mock<IGridAreaRepository>();
            gridAreaRepository.Setup(x => x.GetAsync(new GridAreaId(gridAreaId)))
                .ReturnsAsync(gridArea);

            var eventQueue = new Mock<IGridAreaIntegrationEventsQueueService>();

            var target = new UpdateGridAreaHandler(
                gridAreaRepository.Object,
                eventQueue.Object,
                UnitOfWorkProviderMock.Create(),
                new Mock<IGridAreaAuditLogEntryRepository>().Object,
                CreateMockedUser());

            // act
            await target.Handle(new UpdateGridAreaCommand(gridAreaId, new ChangeGridAreaDto(gridAreaId, "newName")), CancellationToken.None);

            // assert
            eventQueue.Verify(x => x.EnqueueGridAreaNameChangedEventAsync(It.Is<GridArea>(y => y.Name.Value == "newName")), Times.Once);
        }

        [Fact]
        public async Task Handle_NameChanged_LogsOperation()
        {
            // arrange
            var gridAreaId = Guid.NewGuid();
            var gridArea = new GridArea(
                new GridAreaName("name"),
                new GridAreaCode("101"),
                PriceAreaCode.Dk1,
                DateTimeOffset.MinValue,
                null);

            var gridAreaRepository = new Mock<IGridAreaRepository>();
            gridAreaRepository.Setup(x => x.GetAsync(new GridAreaId(gridAreaId)))
                .ReturnsAsync(gridArea);

            var auditLogEntryRepository = new Mock<IGridAreaAuditLogEntryRepository>();

            var target = new UpdateGridAreaHandler(
                gridAreaRepository.Object,
                new Mock<IGridAreaIntegrationEventsQueueService>().Object,
                UnitOfWorkProviderMock.Create(),
                auditLogEntryRepository.Object,
                CreateMockedUser());

            // act
            await target.Handle(new UpdateGridAreaCommand(gridAreaId, new ChangeGridAreaDto(gridAreaId, "newName")), CancellationToken.None);

            // assert
            auditLogEntryRepository.Verify(
                x => x.InsertAsync(
                    It.Is<GridAreaAuditLogEntry>(y => y.Field == GridAreaAuditLogEntryField.Name && y.OldValue == "name" && y.NewValue == "newName")),
                Times.Once);
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
