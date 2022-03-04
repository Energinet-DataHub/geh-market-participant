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
using Energinet.DataHub.MarketParticipant.Domain.Services;
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
            var target = new CreateOrganizationHandler(new Mock<IOrganizationFactoryService>().Object);

            // Act + Assert
            await Assert
                .ThrowsAsync<ArgumentNullException>(() => target.Handle(null!, CancellationToken.None))
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task Handle_NewOrganization_ReturnsOrganizationId()
        {
            // Arrange
            var gln = new GlobalLocationNumber("fake_gln");
            var name = "fake_name";

            var organizationFactoryService = new Mock<IOrganizationFactoryService>();
            var target = new CreateOrganizationHandler(organizationFactoryService.Object);

            var expectedId = Guid.NewGuid();
            var expectedOrganization = new Organization(
                new OrganizationId(expectedId),
                Guid.NewGuid(),
                gln,
                name,
                Array.Empty<IOrganizationRole>());

            organizationFactoryService
                .Setup(x => x.CreateAsync(It.Is<GlobalLocationNumber>(g => g == gln), name))
                .ReturnsAsync(expectedOrganization);

            var command = new CreateOrganizationCommand(new OrganizationDto(name, gln.Value));

            // Act
            var response = await target
                .Handle(command, CancellationToken.None)
                .ConfigureAwait(false);

            // Assert
            Assert.Equal(expectedId.ToString(), response.OrganizationId);
        }
    }
}
