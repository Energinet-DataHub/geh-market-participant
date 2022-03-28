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
using Energinet.DataHub.MarketParticipant.Application.Commands;
using Energinet.DataHub.MarketParticipant.Application.Handlers;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
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
                new Mock<IOrganizationRepository>().Object,
                new Mock<IActorFactoryService>().Object);

            // Act + Assert
            await Assert
                .ThrowsAsync<ArgumentNullException>(() => target.Handle(null!, CancellationToken.None))
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task Handle_NoOrganization_ThrowsNotFoundException()
        {
            // Arrange
            var organizationRepository = new Mock<IOrganizationRepository>();
            var target = new CreateActorHandler(
                organizationRepository.Object,
                new Mock<IActorFactoryService>().Object);

            organizationRepository
                .Setup(x => x.GetAsync(It.IsAny<OrganizationId>()))
                .ReturnsAsync((Organization?)null);

            var command = new CreateActorCommand(
                Guid.Parse("62A79F4A-CB51-4D1E-8B4B-9A9BF3FB2BD4"),
                new ChangeActorDto(new GlobalLocationNumberDto("fake_value"), Array.Empty<MarketRoleDto>()));

            // Act + Assert
            await Assert
                .ThrowsAsync<NotFoundValidationException>(() => target.Handle(command, CancellationToken.None))
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task Handle_NewActor_ActorIdReturned()
        {
            // Arrange
            var organizationRepository = new Mock<IOrganizationRepository>();
            var actorFactory = new Mock<IActorFactoryService>();
            var target = new CreateActorHandler(organizationRepository.Object, actorFactory.Object);

            var orgId = Guid.NewGuid();
            const string orgName = "SomeName";
            var actorId = Guid.NewGuid();
            const string actorGln = "SomeGln";

            var organization = new Organization(new OrganizationId(orgId), orgName, Enumerable.Empty<Actor>());
            var actor = new Actor(new ExternalActorId(actorId), new GlobalLocationNumber(actorGln));

            organizationRepository
                .Setup(x => x.GetAsync(It.Is<OrganizationId>(y => y.Value == orgId)))
                .ReturnsAsync(organization);

            actorFactory
                .Setup(x => x.CreateAsync(
                    organization,
                    It.Is<GlobalLocationNumber>(y => y.Value == actorGln),
                    It.IsAny<IReadOnlyCollection<MarketRole>>()))
                .ReturnsAsync(actor);

            var command = new CreateActorCommand(
                orgId,
                new ChangeActorDto(new GlobalLocationNumberDto(actorGln), Array.Empty<MarketRoleDto>()));

            // Act
            var response = await target
                .Handle(command, CancellationToken.None)
                .ConfigureAwait(false);

            // Assert
            Assert.Equal(actor.Id.ToString(), response.ActorId);
        }
    }
}
