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
    public sealed class CreateOrganizationHandlerTests
    {
        [Fact]
        public async Task Handle_NullArgument_ThrowsException()
        {
            // Arrange
            var target = new CreateOrganizationHandler(new Mock<IOrganizationRepository>().Object);

            // Act + Assert
            await Assert
                .ThrowsAsync<ArgumentNullException>(() => target.Handle(null!, CancellationToken.None))
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task Handle_NewOrganization_ReturnsOrganizationId()
        {
            // Arrange
            var organizationRepository = new Mock<IOrganizationRepository>();
            var target = new CreateOrganizationHandler(organizationRepository.Object);

            var expectedId = Guid.NewGuid();

            organizationRepository
                .Setup(x => x.AddOrUpdateAsync(It.IsAny<Organization>()))
                .ReturnsAsync(new OrganizationId(expectedId));

            var command = new CreateOrganizationCommand(new OrganizationDto(null, "fake_value", "fake_value"));

            // Act
            var response = await target
                .Handle(command, CancellationToken.None)
                .ConfigureAwait(false);

            // Assert
            Assert.Equal(expectedId.ToString(), response.OrganizationId);
        }

        [Fact]
        public async Task Handle_NewOrganization_ValuesAreMapped()
        {
            // Arrange
            var organizationRepository = new Mock<IOrganizationRepository>();
            var target = new CreateOrganizationHandler(organizationRepository.Object);

            var expectedId = Guid.NewGuid();

            const string actorId = "0a7e6621-4a3a-49c4-8b5e-9c7d4d93baae";
            const string orgName = "SomeName";
            const string orgGln = "SomeGln";

            organizationRepository
                .Setup(x => x.AddOrUpdateAsync(It.Is<Organization>(
                    o => o.ActorId.ToString() == actorId && o.Name == orgName && o.Gln.Value == orgGln)))
                .ReturnsAsync(new OrganizationId(expectedId));

            var command = new CreateOrganizationCommand(new OrganizationDto(actorId, orgName, orgGln));

            // Act
            var response = await target
                .Handle(command, CancellationToken.None)
                .ConfigureAwait(false);

            // Assert
            Assert.Equal(expectedId.ToString(), response.OrganizationId);
        }
    }
}
