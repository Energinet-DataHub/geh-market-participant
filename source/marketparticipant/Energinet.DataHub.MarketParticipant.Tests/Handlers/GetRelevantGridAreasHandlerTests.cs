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
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.Core.App.Common.Abstractions.Users;
using Energinet.DataHub.MarketParticipant.Application.Commands.GridAreas;
using Energinet.DataHub.MarketParticipant.Application.Handlers.GridAreas;
using Energinet.DataHub.MarketParticipant.Application.Security;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Tests.Common;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Handlers;

[UnitTest]
public sealed class GetRelevantGridAreasHandlerTests
{
    [Fact]
    public async Task Handle_ActorWithGridAreas_ReturnsGridAreas()
    {
        // arrange
        var gridArea = new GridArea(
                    new GridAreaId(Guid.NewGuid()),
                    new GridAreaName("name"),
                    new GridAreaCode("code"),
                    PriceAreaCode.Dk1,
                    GridAreaType.Distribution,
                    DateTimeOffset.MinValue,
                    DateTimeOffset.MaxValue);
        var gridAreaRepositoryMock = new Mock<IGridAreaRepository>();
        gridAreaRepositoryMock
            .Setup(x => x.GetAsync())
            .ReturnsAsync([gridArea]);

        var mockedActor = TestPreparationModels.MockedActor(Guid.NewGuid(), Guid.NewGuid());
        mockedActor.UpdateMarketRole(new ActorMarketRole(mockedActor.MarketRole.Function, [new ActorGridArea(gridArea.Id, [])]));
        var actorRepositoryMock = new Mock<IActorRepository>();
        actorRepositoryMock.Setup(x => x.GetAsync(It.IsAny<ActorId>()))
            .ReturnsAsync(mockedActor);

        var userContextMock = new Mock<IUserContext<FrontendUser>>();
        userContextMock
            .Setup(x => x.CurrentUser)
            .Returns(new FrontendUser(Guid.NewGuid(), mockedActor.OrganizationId.Value, mockedActor.Id.Value, true));

        var relevantGridAreasRequest = new GetRelevantGridAreasRequestDto(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2024, 1, 31, 23, 59, 59, TimeSpan.Zero));

        var target = new GetRelevantGridAreasHandler(actorRepositoryMock.Object, gridAreaRepositoryMock.Object, userContextMock.Object);

        // act
        var actual = await target.Handle(new GetRelevantGridAreasCommand(relevantGridAreasRequest), CancellationToken.None);

        // assert
        Assert.NotEmpty(actual.GridAreas);
    }

    [Fact]
    public async Task Handle_ActorHasNoGridAreas_ReturnsEmpty()
    {
        // arrange
        var gridArea = new GridArea(
                    new GridAreaId(Guid.NewGuid()),
                    new GridAreaName("name"),
                    new GridAreaCode("code"),
                    PriceAreaCode.Dk1,
                    GridAreaType.Distribution,
                    DateTimeOffset.MinValue,
                    DateTimeOffset.MaxValue);
        var gridAreaRepositoryMock = new Mock<IGridAreaRepository>();
        gridAreaRepositoryMock
            .Setup(x => x.GetAsync())
            .ReturnsAsync([gridArea]);

        var mockedActor = TestPreparationModels.MockedActor(Guid.NewGuid(), Guid.NewGuid());
        var actorRepositoryMock = new Mock<IActorRepository>();
        actorRepositoryMock.Setup(x => x.GetAsync(It.IsAny<ActorId>()))
            .ReturnsAsync(mockedActor);

        var userContextMock = new Mock<IUserContext<FrontendUser>>();
        userContextMock
            .Setup(x => x.CurrentUser)
            .Returns(new FrontendUser(Guid.NewGuid(), mockedActor.OrganizationId.Value, mockedActor.Id.Value, true));

        var relevantGridAreasRequest = new GetRelevantGridAreasRequestDto(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2024, 1, 31, 23, 59, 59, TimeSpan.Zero));

        var target = new GetRelevantGridAreasHandler(actorRepositoryMock.Object, gridAreaRepositoryMock.Object, userContextMock.Object);

        // act
        var actual = await target.Handle(new GetRelevantGridAreasCommand(relevantGridAreasRequest), CancellationToken.None);

        // assert
        Assert.Empty(actual.GridAreas);
    }

    [Theory]
    [InlineData("01/01/2023 00:00:00", "31/12/2025 23:59:59", "01/01/2024 00:00:00", "31/01/2024 23:59:59")]
    [InlineData("01/01/2023 00:00:00", "31/12/2025 23:59:59", "01/01/2022 00:00:00", "31/01/2023 23:59:59")]
    [InlineData("01/01/2023 00:00:00", "31/12/2025 23:59:59", "01/01/2022 00:00:00", "31/01/2026 23:59:59")]
    [InlineData("01/01/2023 00:00:00", null, "01/01/2022 00:00:00", "31/01/2026 23:59:59")]
    [InlineData("01/01/2023 00:00:00", null, "31/12/2025 23:59:59", "31/01/2026 23:59:59")]
    public async Task Handle_ValidDates_ReturnsGridAreas(string validFrom, string? validTo, string periodStart, string periodEnd)
    {
        // arrange
        var gridArea = new GridArea(
                    new GridAreaId(Guid.NewGuid()),
                    new GridAreaName("name"),
                    new GridAreaCode("code"),
                    PriceAreaCode.Dk1,
                    GridAreaType.Distribution,
                    DateTimeOffset.Parse(validFrom, new CultureInfo("da-dk")),
                    !string.IsNullOrEmpty(validTo) ? DateTimeOffset.Parse(validTo, new CultureInfo("da-dk")) : null);
        var gridAreaRepositoryMock = new Mock<IGridAreaRepository>();
        gridAreaRepositoryMock
            .Setup(x => x.GetAsync())
            .ReturnsAsync([gridArea]);

        var mockedActor = TestPreparationModels.MockedActor(Guid.NewGuid(), Guid.NewGuid());
        mockedActor.UpdateMarketRole(new ActorMarketRole(mockedActor.MarketRole.Function, [new ActorGridArea(gridArea.Id, [])]));
        var actorRepositoryMock = new Mock<IActorRepository>();
        actorRepositoryMock.Setup(x => x.GetAsync(It.IsAny<ActorId>()))
            .ReturnsAsync(mockedActor);

        var userContextMock = new Mock<IUserContext<FrontendUser>>();
        userContextMock
            .Setup(x => x.CurrentUser)
            .Returns(new FrontendUser(Guid.NewGuid(), mockedActor.OrganizationId.Value, mockedActor.Id.Value, true));

        var relevantGridAreasRequest = new GetRelevantGridAreasRequestDto(DateTimeOffset.Parse(periodStart, new CultureInfo("da-dk")), DateTimeOffset.Parse(periodEnd, new CultureInfo("da-dk")));

        var target = new GetRelevantGridAreasHandler(actorRepositoryMock.Object, gridAreaRepositoryMock.Object, userContextMock.Object);

        // act
        var actual = await target.Handle(new GetRelevantGridAreasCommand(relevantGridAreasRequest), CancellationToken.None);

        // assert
        Assert.NotEmpty(actual.GridAreas);
    }

    [Theory]
    [InlineData("01/01/2023 00:00:00", "31/12/2025 23:59:59", "01/01/2022 00:00:00", "31/01/2022 23:59:59")]
    [InlineData("01/01/2023 00:00:00", "31/12/2025 23:59:59", "01/01/2026 00:00:00", "31/01/2026 23:59:59")]
    [InlineData("01/01/2027 00:00:00", null, "31/12/2025 23:59:59", "31/01/2026 23:59:59")]
    public async Task Handle_InvalidDates_ReturnsEmpty(string validFrom, string? validTo, string periodStart, string periodEnd)
    {
        // arrange
        var gridArea = new GridArea(
                    new GridAreaId(Guid.NewGuid()),
                    new GridAreaName("name"),
                    new GridAreaCode("code"),
                    PriceAreaCode.Dk1,
                    GridAreaType.Distribution,
                    DateTimeOffset.Parse(validFrom, new CultureInfo("da-dk")),
                    !string.IsNullOrEmpty(validTo) ? DateTimeOffset.Parse(validTo, new CultureInfo("da-dk")) : null);
        var gridAreaRepositoryMock = new Mock<IGridAreaRepository>();
        gridAreaRepositoryMock
            .Setup(x => x.GetAsync())
            .ReturnsAsync([gridArea]);

        var mockedActor = TestPreparationModels.MockedActor(Guid.NewGuid(), Guid.NewGuid());
        mockedActor.UpdateMarketRole(new ActorMarketRole(mockedActor.MarketRole.Function, [new ActorGridArea(gridArea.Id, [])]));
        var actorRepositoryMock = new Mock<IActorRepository>();
        actorRepositoryMock.Setup(x => x.GetAsync(It.IsAny<ActorId>()))
            .ReturnsAsync(mockedActor);

        var userContextMock = new Mock<IUserContext<FrontendUser>>();
        userContextMock
            .Setup(x => x.CurrentUser)
            .Returns(new FrontendUser(Guid.NewGuid(), mockedActor.OrganizationId.Value, mockedActor.Id.Value, true));

        var relevantGridAreasRequest = new GetRelevantGridAreasRequestDto(DateTimeOffset.Parse(periodStart, new CultureInfo("da-dk")), DateTimeOffset.Parse(periodEnd, new CultureInfo("da-dk")));

        var target = new GetRelevantGridAreasHandler(actorRepositoryMock.Object, gridAreaRepositoryMock.Object, userContextMock.Object);

        // act
        var actual = await target.Handle(new GetRelevantGridAreasCommand(relevantGridAreasRequest), CancellationToken.None);

        // assert
        Assert.Empty(actual.GridAreas);
    }
}
