using System;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories
{
    [IntegrationTest]
    public sealed class OrganziationRepositoryTests
    {

        [Fact]
        public async Task SaveAsync_OneOrganization_CanReadBack()
        {
            // Arrange
            await using var host = await OrganizationHost.InitializeAsync().ConfigureAwait(false);
            await using var scope = host.BeginScope();
            var orgRepository = scope.GetInstance<IOrganizationRepository>();
            var testOrg = new Organization(
                new Uuid(Guid.NewGuid()),
                new GlobalLocationNumber("123"),
                "Test"
            );

            // Act
            await orgRepository.SaveAsync(testOrg);

            Assert.True(true);
        }
    }

}
