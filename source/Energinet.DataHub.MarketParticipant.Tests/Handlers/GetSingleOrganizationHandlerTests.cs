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
using Energinet.DataHub.MarketParticipant.Application.Commands;
using Energinet.DataHub.MarketParticipant.Application.Handlers;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Handlers
{
    [UnitTest]
    public sealed class GetSingleOrganizationHandlerTests
    {
        [Fact]
        public async Task Handle_HasOrganization_ReturnsOrganization()
        {
            // Arrange
            var organizationRepository = new Mock<IOrganizationRepository>();
            var target = new GetSingleOrganizationHandler(organizationRepository.Object);

            var marketRole = new MarketRole(EicFunction.BalanceResponsibleParty);

            var actor = new Actor(
                Guid.NewGuid(),
                new ExternalActorId(Guid.NewGuid()),
                new GlobalLocationNumber("fake_value"),
                ActorStatus.Active,
                Enumerable.Empty<GridArea>(),
                new[] { marketRole },
                Enumerable.Empty<MeteringPointType>());

            var organization = new Organization(
                new OrganizationId(Guid.NewGuid()),
                "fake_value",
                new[] { actor });

            organizationRepository
                .Setup(x => x.GetAsync(It.IsAny<OrganizationId>()))
                .ReturnsAsync(organization);

            var command = new GetSingleOrganizationCommand(organization.Id);

            // Act
            var response = await target
                .Handle(command, CancellationToken.None)
                .ConfigureAwait(false);

            // Assert
            Assert.True(response.OrganizationFound);
            Assert.NotNull(response.Organization);

            var actualOrganization = response.Organization!;
            Assert.Equal(organization.Id.ToString(), actualOrganization.OrganizationId);

            var actualActor = actualOrganization.Actors.Single();
            Assert.Equal(actor.Id.ToString(), actualActor.ActorId);
            Assert.Equal(actor.ExternalActorId.ToString(), actualActor.ExternalActorId);
            Assert.Equal(actor.Gln.Value, actualActor.Gln.Value);
            Assert.Equal(actor.Status.ToString(), actualActor.Status);

            var actualMarketRole = actualActor.MarketRoles.Single();
            Assert.Equal(marketRole.Function.ToString(), actualMarketRole.Function);
        }
    }
}
