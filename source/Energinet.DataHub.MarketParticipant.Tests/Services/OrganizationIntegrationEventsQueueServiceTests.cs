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
using Energinet.DataHub.MarketParticipant.Application.Commands.Organization;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.IntegrationEvents;
using Energinet.DataHub.MarketParticipant.Domain.Model.IntegrationEvents.OrganizationIntegrationEvents;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Services
{
    [UnitTest]
    public sealed class OrganizationIntegrationEventsQueueServiceTests
    {
        [Fact]
        public Task EnqueueOrganizationNameChangedEventAsync_CreatesEvent()
        {
            // Arrange
            var helper = new OrganizationIntegrationEventsHelperService();

            var organisationDomainModel = new Organization(
                new OrganizationId(Guid.NewGuid()),
                "Old Name",
                Enumerable.Empty<Actor>(),
                new BusinessRegisterIdentifier("12345678"),
                new Address(
                    "fake_value",
                    "fake_value",
                    "fake_value",
                    "fake_value",
                    "fake_value"),
                new OrganizationDomain("energinet.dk"),
                "Test Comment",
                OrganizationStatus.Active);

            var organisationDto = new ChangeOrganizationDto(
                "New Name",
                "12345678",
                new AddressDto(
                    "fake_value",
                    "fake_value",
                    "fake_value",
                    "fake_value",
                    "fake_value"),
                "Test Comment",
                "Active");

            // Act
            var changeEvents = helper.DetermineOrganizationUpdatedChangeEvents(organisationDomainModel, organisationDto);

            // Assert
            var integrationEvents = changeEvents.ToList();
            Assert.Single(integrationEvents);
            Assert.Contains(integrationEvents, e => e is OrganizationNameChangedIntegrationEvent);
            return Task.CompletedTask;
        }

        [Fact]
        public Task EnqueueOrganizationStatusChangedEventAsync_CreatesEvent()
        {
            // Arrange
            var helper = new OrganizationIntegrationEventsHelperService();

            var organisationDomainModel = new Organization(
                new OrganizationId(Guid.NewGuid()),
                "Name",
                Enumerable.Empty<Actor>(),
                new BusinessRegisterIdentifier("12345678"),
                new Address(
                    "fake_value",
                    "fake_value",
                    "fake_value",
                    "fake_value",
                    "fake_value"),
                new OrganizationDomain("energinet.dk"),
                "Test Comment",
                OrganizationStatus.Active);

            var organisationDto = new ChangeOrganizationDto(
                "Name",
                "12345678",
                new AddressDto(
                    "fake_value",
                    "fake_value",
                    "fake_value",
                    "fake_value",
                    "fake_value"),
                "Test Comment",
                "Blocked");

            // Act
            var changeEvents = helper.DetermineOrganizationUpdatedChangeEvents(organisationDomainModel, organisationDto);

            // Assert
            var integrationEvents = changeEvents.ToList();
            Assert.Single(integrationEvents);
            Assert.Contains(integrationEvents, e => e is OrganizationStatusChangedIntegrationEvent);
            return Task.CompletedTask;
        }
    }
}
