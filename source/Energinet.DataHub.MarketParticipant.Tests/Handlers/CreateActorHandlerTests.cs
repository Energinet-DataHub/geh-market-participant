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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Actor;
using Energinet.DataHub.MarketParticipant.Application.Handlers;
using Energinet.DataHub.MarketParticipant.Application.Handlers.Actor;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Handlers
{
    [UnitTest]
    public sealed class CreateActorHandlerTests
    {
        [Fact]
        public async Task Handle_NullArgument_ThrowsException()
        {
            // Arrange
            var target = new CreateActorHandler(
                new Mock<IOrganizationExistsHelperService>().Object,
                new Mock<IActorFactoryService>().Object);

            // Act + Assert
            await Assert
                .ThrowsAsync<ArgumentNullException>(() => target.Handle(null!, CancellationToken.None))
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task Handle_NewActor_ActorIdReturned()
        {
            // Arrange
            const string orgName = "SomeName";
            const string actorGln = "SomeGln";
            var organizationExistsHelperService = new Mock<IOrganizationExistsHelperService>();
            var actorFactory = new Mock<IActorFactoryService>();
            var target = new CreateActorHandler(organizationExistsHelperService.Object, actorFactory.Object);
            var orgId = Guid.NewGuid();
            var actorId = Guid.NewGuid();
            var validCvr = new CVRNumber("123");
            var validAddress = new Address(
                "test Street",
                "1",
                "1111",
                "Test City",
                "Test Country");

            var organization = new Organization(new OrganizationId(orgId), orgName, Enumerable.Empty<Actor>(), validCvr, validAddress);
            var actor = new Actor(new ExternalActorId(actorId), new GlobalLocationNumber(actorGln));

            organizationExistsHelperService
                .Setup(x => x.EnsureOrganizationExistsAsync(orgId))
                .ReturnsAsync(organization);

            actorFactory
                .Setup(x => x.CreateAsync(
                    organization,
                    It.Is<GlobalLocationNumber>(y => y.Value == actorGln),
                    It.IsAny<IReadOnlyCollection<MarketRole>>()))
                .ReturnsAsync(actor);

            var command = new CreateActorCommand(
                orgId,
                new CreateActorDto(new GlobalLocationNumberDto(actorGln), Array.Empty<MarketRoleDto>()));

            // Act
            var response = await target
                .Handle(command, CancellationToken.None)
                .ConfigureAwait(false);

            // Assert
            Assert.Equal(actor.Id.ToString(), response.ActorId);
        }

        [Fact]
        public async Task Handle_NewActorWithMarketRoles_ActorIdReturned()
        {
            // Arrange
            const string orgName = "SomeName";
            const string actorGln = "SomeGln";
            var organizationExistsHelperService = new Mock<IOrganizationExistsHelperService>();
            var actorFactory = new Mock<IActorFactoryService>();
            var target = new CreateActorHandler(organizationExistsHelperService.Object, actorFactory.Object);
            var orgId = Guid.NewGuid();
            var actorId = Guid.NewGuid();
            var validCvr = new CVRNumber("123");
            var validAddress = new Address(
                "test Street",
                "1",
                "1111",
                "Test City",
                "Test Country");

            var organization = new Organization(new OrganizationId(orgId), orgName, Enumerable.Empty<Actor>(), validCvr, validAddress);
            var actor = new Actor(new ExternalActorId(actorId), new GlobalLocationNumber(actorGln));
            var marketRole = new MarketRoleDto(EicFunction.BillingAgent.ToString());

            organizationExistsHelperService
                .Setup(x => x.EnsureOrganizationExistsAsync(orgId))
                .ReturnsAsync(organization);

            actorFactory
                .Setup(x => x.CreateAsync(
                    organization,
                    It.Is<GlobalLocationNumber>(y => y.Value == actorGln),
                    It.IsAny<IReadOnlyCollection<MarketRole>>()))
                .ReturnsAsync(actor);

            var command = new CreateActorCommand(
                orgId,
                new CreateActorDto(new GlobalLocationNumberDto(actorGln), new[] { marketRole }));

            // Act
            var response = await target
                .Handle(command, CancellationToken.None)
                .ConfigureAwait(false);

            // Assert
            Assert.Equal(actor.Id.ToString(), response.ActorId);
        }
    }
}
