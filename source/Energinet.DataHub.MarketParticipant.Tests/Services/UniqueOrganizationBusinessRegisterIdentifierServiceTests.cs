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

using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Moq;
using Xunit;

namespace Energinet.DataHub.MarketParticipant.Tests.Services
{
    public sealed class UniqueOrganizationBusinessRegisterIdentifierServiceTests
    {
        [Fact]
        public async Task Ensure_BusinessRegisterIdentifierNotUnique_Throws()
        {
            // arrange
            var organization = new Organization(
                "org",
                new BusinessRegisterIdentifier("fake_value"),
                new Address(string.Empty, string.Empty, string.Empty, string.Empty, "DK"));

            var repository = new Mock<IOrganizationRepository>();
            repository.Setup(x => x.GetAsync()).ReturnsAsync(new[] { organization });

            var target = new UniqueOrganizationBusinessRegisterIdentifierService(repository.Object);

            // act + assert
            await Assert.ThrowsAsync<ValidationException>(
                () => target.EnsureUniqueMarketRolesPerGridAreaAsync(organization));
        }

        [Fact]
        public async Task Ensure_BusinessRegisterIdentifierUnique_DoesNotThrow()
        {
            // arrange
            var existingOrganisation = new Organization(
                "org",
                new BusinessRegisterIdentifier("fake_value"),
                new Address(string.Empty, string.Empty, string.Empty, string.Empty, "DK"));

            var repository = new Mock<IOrganizationRepository>();
            repository.Setup(x => x.GetAsync()).ReturnsAsync(new[] { existingOrganisation });

            var organization = new Organization(
                "org",
                new BusinessRegisterIdentifier("unique"),
                new Address(string.Empty, string.Empty, string.Empty, string.Empty, "DK"));

            var target = new UniqueOrganizationBusinessRegisterIdentifierService(repository.Object);

            // act + assert
            await target.EnsureUniqueMarketRolesPerGridAreaAsync(organization);
        }
    }
}
