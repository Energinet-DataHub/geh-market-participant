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
using Energinet.DataHub.MarketParticipant.Application.Commands.Organization;
using Energinet.DataHub.MarketParticipant.Application.Handlers.Organization;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Tests.Common;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Handlers
{
    [UnitTest]
    public sealed class GetOrganizationsAuditLogsHandlerTests
    {
        [Fact]
        public async Task Handle_HasAuditLogs_ReturnsLogs()
        {
            // Arrange
            var targetRepository = new Mock<IOrganizationAuditLogEntryRepository>();
            var target = new GetOrganizationAuditLogsHandler(targetRepository.Object);
            var organization = TestPreparationModels.MockedOrganization();

            targetRepository
                .Setup(x => x.GetAsync(It.IsAny<OrganizationId>()))
                .ReturnsAsync(new[]
                {
                    new OrganizationAuditLogEntry(
                    organization.Id,
                    KnownAuditIdentityProvider.TestFramework.IdentityId,
                    OrganizationChangeType.Name,
                    DateTimeOffset.UtcNow,
                    "new Name"),
                    new OrganizationAuditLogEntry(
                        organization.Id,
                        KnownAuditIdentityProvider.TestFramework.IdentityId,
                        OrganizationChangeType.AddressCity,
                        DateTimeOffset.UtcNow,
                        "new City 2")
                });

            var command = new GetOrganizationAuditLogsCommand(organization.Id.Value);

            // Act
            var response = await target.Handle(command, CancellationToken.None);

            // Assert
            Assert.NotEmpty(response.OrganizationAuditLogs);
            Assert.Equal(2, response.OrganizationAuditLogs.Count());
            Assert.Contains(response.OrganizationAuditLogs, x => x.OrganizationChangeType == OrganizationChangeType.Name);
            Assert.Contains(response.OrganizationAuditLogs, x => x.OrganizationChangeType == OrganizationChangeType.AddressCity);
            Assert.Contains(response.OrganizationAuditLogs, x => x.Value == "new Name");
            Assert.Contains(response.OrganizationAuditLogs, x => x.Value == "new City 2");
        }
    }
}
