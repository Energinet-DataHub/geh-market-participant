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
using Energinet.DataHub.MarketParticipant.Application.Commands;
using Energinet.DataHub.MarketParticipant.Application.Handlers;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Handlers
{
    [UnitTest]
    public sealed class UpdateOrganizationHandlerTests
    {
        [Fact]
        public async Task Handle_UpdateOrganization_ReturnsOk()
        {
            // Arrange
            var organizationRepository = new Mock<IOrganizationRepository>();
            var target = new UpdateOrganizationHandler(organizationRepository.Object);
            const string orgId = "1572cb86-3c1d-4899-8d7a-983d8de0796b";

            var marketRole = new MarketRole(EicFunction.BalanceResponsibleParty);

            var actor = new Actor(
                Guid.NewGuid(),
                new ExternalActorId(Guid.NewGuid()),
                new GlobalLocationNumber("fake_value"),
                ActorStatus.Active,
                Enumerable.Empty<GridArea>(),
                new[] { marketRole },
                Enumerable.Empty<MeteringPointType>());

            var organization = new Organization(
                new OrganizationId(orgId),
                "fake_value",
                new[] { actor });

            organizationRepository
                .Setup(x => x.GetAsync(It.IsAny<OrganizationId>()))
                .ReturnsAsync(organization);
            organizationRepository
                .Setup(x => x.AddOrUpdateAsync(It.IsAny<Organization>()))
                .ReturnsAsync(new OrganizationId(orgId));

            var changeDto = new ChangeOrganizationDto("New name");

            var command = new UpdateOrganizationCommand(Guid.Parse(orgId), changeDto);

            // Act
            var response = await target
                .Handle(command, CancellationToken.None)
                .ConfigureAwait(false);

            // Assert
            Assert.Equal(Unit.Value, response);
        }
    }
}
