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
using Energinet.DataHub.MarketParticipant.Application.Commands.Organization;
using Energinet.DataHub.MarketParticipant.Application.Handlers.Organization;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Energinet.DataHub.MarketParticipant.Tests.Common;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Handlers
{
    [UnitTest]
    public sealed class CreateOrganizationHandlerTests
    {
        [Fact]
        public async Task Handle_NewOrganization_ReturnsOrganizationId()
        {
            // Arrange
            var orgFactory = new Mock<IOrganizationFactoryService>();
            var target = new CreateOrganizationHandler(orgFactory.Object);
            var validAddressDto = new AddressDto(
                "test Street",
                "1",
                "1111",
                "Test City",
                "Test Country");
            const string validCvr = "123";
            const string orgName = "fake_value";

            var organization = TestPreparationModels.MockedOrganization();

            orgFactory
                .Setup(x => x.CreateAsync(
                    It.IsAny<string>(),
                    It.Is<BusinessRegisterIdentifier>(y => y.Identifier == validCvr),
                    It.IsAny<Address>(),
                    It.IsAny<OrganizationDomain>(),
                    It.IsAny<string>()))
                .ReturnsAsync(organization);

            var command = new CreateOrganizationCommand(new CreateOrganizationDto(orgName, validCvr, validAddressDto, "energinet.dk", "Test Comment"));

            // Act
            var response = await target.Handle(command, CancellationToken.None);

            // Assert
            Assert.NotEqual(Guid.Empty, response.OrganizationId);
        }
    }
}
