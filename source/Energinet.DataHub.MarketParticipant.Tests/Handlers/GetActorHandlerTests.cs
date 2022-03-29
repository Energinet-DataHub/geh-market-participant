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
using Energinet.DataHub.MarketParticipant.Application.Commands.Actor;
using Energinet.DataHub.MarketParticipant.Application.Handlers;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Handlers
{
    [UnitTest]
    public sealed class GetActorHandlerTests
    {
        [Fact]
        public async Task Handle_NullArgument_ThrowsException()
        {
            // Arrange
            var target = new GetActorHandler(new Mock<IOrganizationRepository>().Object);

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
            var target = new GetActorHandler(organizationRepository.Object);

            organizationRepository
                .Setup(x => x.GetAsync(It.IsAny<OrganizationId>()))
                .ReturnsAsync((Organization?)null);

            var command = new GetSingleActorCommand(Guid.NewGuid(), Guid.NewGuid());

            // Act + Assert
            await Assert
                .ThrowsAsync<NotFoundValidationException>(() => target.Handle(command, CancellationToken.None))
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task Handle_NoActor_ThrowsNotFoundException()
        {
            // Arrange
            var organizationRepository = new Mock<IOrganizationRepository>();
            var target = new GetActorHandler(organizationRepository.Object);

            var orgId = Guid.NewGuid();
            const string orgName = "SomeName";

            var organization = new Organization(new OrganizationId(orgId), orgName, Enumerable.Empty<Actor>());

            organizationRepository
                .Setup(x => x.GetAsync(It.IsAny<OrganizationId>()))
                .ReturnsAsync(organization);

            var command = new GetSingleActorCommand(Guid.NewGuid(), orgId);

            // Act + Assert
            await Assert
                .ThrowsAsync<NotFoundValidationException>(() => target.Handle(command, CancellationToken.None))
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task Handle_HasActor_ReturnsActor()
        {
            // Arrange
            var organizationRepository = new Mock<IOrganizationRepository>();
            var target = new GetActorHandler(organizationRepository.Object);

            var orgId = Guid.NewGuid();
            const string orgName = "SomeName";
            var actorId = Guid.NewGuid();
            const string actorGln = "SomeGln";

            var actor = new Actor(new ExternalActorId(actorId), new GlobalLocationNumber(actorGln));
            var organization = new Organization(new OrganizationId(orgId), orgName, new[] { actor });

            organizationRepository
                .Setup(x => x.GetAsync(It.IsAny<OrganizationId>()))
                .ReturnsAsync(organization);

            var command = new GetSingleActorCommand(Guid.NewGuid(), Guid.NewGuid());

            // Act
            var response = await target.Handle(command, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.NotNull(response.Actor);
        }
    }
}
