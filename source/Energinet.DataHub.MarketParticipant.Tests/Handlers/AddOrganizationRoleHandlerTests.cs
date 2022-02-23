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
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Handlers
{
    [UnitTest]
    public sealed class AddOrganizationRoleHandlerTests
    {
        [Fact]
        public async Task Handle_NullArgument_ThrowsException()
        {
            // Arrange
            var target = new AddOrganizationRoleHandler(new Mock<IOrganizationRepository>().Object);

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
            var target = new AddOrganizationRoleHandler(organizationRepository.Object);

            organizationRepository
                .Setup(x => x.GetAsync(It.IsAny<OrganizationId>()))
                .ReturnsAsync((Organization?)null);

            var command = new AddOrganizationRoleCommand("62A79F4A-CB51-4D1E-8B4B-9A9BF3FB2BD4", new OrganizationRoleDto("ddq"));

            // Act + Assert
            await Assert
                .ThrowsAsync<NotFoundValidationException>(() => target.Handle(command, CancellationToken.None))
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task Handle_NewOrganization_ValuesAreMapped()
        {
            // Arrange
            var organizationRepository = new Mock<IOrganizationRepository>();
            var target = new AddOrganizationRoleHandler(organizationRepository.Object);

            var expectedId = Guid.NewGuid();

            const string actorId = "0A7E6621-4A3A-49C4-8B5E-9C7D4D93BAAE";
            const string role = "ddm";
            const string orgId = "E5E61770-7526-444C-948A-23FFA5B6517D";
            const string orgName = "SomeName";
            const string orgGln = "SomeGln";

            organizationRepository
                .Setup(x => x.GetAsync(It.Is<OrganizationId>(y => y.Value == new Guid(orgId))))
                .ReturnsAsync(new Organization(new Guid(actorId), new GlobalLocationNumber(orgGln), orgName));

            organizationRepository
                .Setup(x => x.AddOrUpdateAsync(It.Is<Organization>(
                    o => o.ActorId.ToString() == actorId && o.Name == orgName && o.Gln.Value == orgGln)))
                .ReturnsAsync(new OrganizationId(expectedId));

            var command = new AddOrganizationRoleCommand(orgId, new OrganizationRoleDto(role));

            // Act
            await target
                .Handle(command, CancellationToken.None)
                .ConfigureAwait(false);

            // Assert
            organizationRepository.Verify(
                x => x.AddOrUpdateAsync(It.Is<Organization>(
                    y => y.Roles.Count(z => z.Code == BusinessRoleCode.Ddm) == 1)),
                Times.Once);
        }
    }
}
