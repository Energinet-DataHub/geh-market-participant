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

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Organization;
using Energinet.DataHub.MarketParticipant.Application.Handlers.Organization;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Tests.Common;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Handlers
{
    [UnitTest]
    public sealed class GetOrganizationsHandlerTests
    {
        [Fact]
        public async Task Handle_HasOrganization_ReturnsOrganization()
        {
            // Arrange
            var organizationRepository = new Mock<IOrganizationRepository>();
            var target = new GetOrganizationsHandler(organizationRepository.Object);

            var organization = TestPreparationModels.MockedOrganization();

            organizationRepository
                .Setup(x => x.GetAsync())
                .ReturnsAsync(new[] { organization });

            var command = new GetOrganizationsCommand(null);

            // Act
            var response = await target.Handle(command, CancellationToken.None);

            // Assert
            Assert.NotEmpty(response.Organizations);

            var actualOrganization = response.Organizations.Single();
            Assert.Equal(organization.Id.Value, actualOrganization.OrganizationId);
        }
    }
}
