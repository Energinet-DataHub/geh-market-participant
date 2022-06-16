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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Services
{
    [UnitTest]
    public sealed class OrganizationIntegrationEventsQueueServiceTests
    {
        [Fact]
        public async Task EnqueueOrganizationUpdatedEventAsync_CreatesEvent()
        {
            // Arrange
            var domainEventRepository = new Mock<IDomainEventRepository>();
            var target = new OrganizationIntegrationEventsQueueService(domainEventRepository.Object);
            var helper = new OrganizationIntegrationEventsHelperService();

            var organizationArea = new Organization(
                new OrganizationId(Guid.NewGuid()),
                "fake_value",
                Enumerable.Empty<Actor>(),
                new BusinessRegisterIdentifier("12345678"),
                new Address(
                    "fake_value",
                    "fake_value",
                    "fake_value",
                    "fake_value",
                    "fake_value"),
                "Test Comment");

            var changeEvent = helper.BuildOrganizationCreatedEvents(organizationArea);

            // Act
            await target.EnqueueOrganizationIntegrationEventAsync(changeEvent).ConfigureAwait(false);

            // Assert
            domainEventRepository.Verify(
                x => x.InsertAsync(It.Is<DomainEvent>(y => y.DomainObjectId == organizationArea.Id.Value)),
                Times.Once);
        }
    }
}
