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
using System.Security.Claims;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Security;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Tests.Common;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Security;

[UnitTest]
public sealed class FrontendUserProviderTests
{
    [Fact]
    public async Task ProvideUserAsync_UnknownActor_ReturnsNull()
    {
        // Arrange
        var actorRepositoryMock = new Mock<IActorRepository>();

        var target = new FrontendUserProvider(actorRepositoryMock.Object);

        // Act
        var actual = await target.ProvideUserAsync(Guid.NewGuid(), Guid.NewGuid(), false, Array.Empty<Claim>());

        // Assert
        Assert.Null(actual);
    }

    [Theory]
    [InlineData(ActorStatus.Inactive, false)]
    [InlineData(ActorStatus.New, false)]
    [InlineData(ActorStatus.Passive, true)]
    [InlineData(ActorStatus.Active, true)]
    public async Task ProvideUserAsync_IncorrectActorStatus_ReturnsNull(ActorStatus actorStatus, bool isValid)
    {
        // Arrange
        var actorId = Guid.NewGuid();
        var actorRepositoryMock = new Mock<IActorRepository>();
        actorRepositoryMock
            .Setup(actorRepository => actorRepository.GetAsync(new ActorId(actorId)))
            .ReturnsAsync(new Actor(
                new ActorId(Guid.NewGuid()),
                new OrganizationId(Guid.NewGuid()),
                null,
                new MockedGln(),
                actorStatus,
                Array.Empty<ActorMarketRole>(),
                new ActorName(string.Empty)));

        var target = new FrontendUserProvider(actorRepositoryMock.Object);

        // Act + Assert
        if (isValid)
        {
            Assert.NotNull(await target.ProvideUserAsync(Guid.NewGuid(), actorId, false, Array.Empty<Claim>()));
        }
        else
        {
            Assert.Null(await target.ProvideUserAsync(Guid.NewGuid(), actorId, false, Array.Empty<Claim>()));
        }
    }
}
