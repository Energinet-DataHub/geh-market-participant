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
using Energinet.DataHub.MarketParticipant.Application.Commands.GridArea;
using Energinet.DataHub.MarketParticipant.Application.Handlers.GridArea;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Handlers
{
    [UnitTest]
    public sealed class CreateGridAreaHandlerTests
    {
        [Fact]
        public async Task Handle_NewGridArea_GridAreaReturned()
        {
            // Arrange
            var gridAreaFactoryService = new Mock<IGridAreaFactoryService>();
            var gridArea = new GridArea(
                new GridAreaName("name"),
                new GridAreaCode("101"),
                PriceAreaCode.Dk1,
                DateTimeOffset.MinValue,
                null);

            gridAreaFactoryService
                .Setup(x => x.CreateAsync(
                    It.Is<GridAreaCode>(y => y.Value == "101"),
                    It.Is<GridAreaName>(y => y.Value == "name"),
                    It.IsAny<PriceAreaCode>(),
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<DateTimeOffset?>()))
                .ReturnsAsync(gridArea);

            var target = new CreateGridAreaHandler(gridAreaFactoryService.Object);
            var command = new CreateGridAreaCommand(new CreateGridAreaDto(
                "name",
                "101",
                "Dk1"));

            // Act
            var response = await target
                .Handle(command, CancellationToken.None)
                .ConfigureAwait(false);

            // Assert
            Assert.Equal(gridArea.Id.Value, response.GridAreaId.Value);
        }
    }
}
