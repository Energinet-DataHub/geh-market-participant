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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Categories;
namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories
{
    [Collection("IntegrationTest")]
    [IntegrationTest]
    public sealed class OrganizationRepositoryTests
    {
        private readonly MarketParticipantDatabaseFixture _fixture;
        private readonly Address _validAddress = new(
            "test Street",
            "1",
            "1111",
            "Test City",
            "Test Country");
        private readonly OrganizationDomain _validDomain = new(new MockedDomain());

        public OrganizationRepositoryTests(MarketParticipantDatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task AddOrUpdateAsync_OneOrganization_CanReadBack()
        {
            // Arrange
            await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();
            await using var context2 = _fixture.DatabaseManager.CreateDbContext();
            var orgRepository = new OrganizationRepository(context);
            var orgRepository2 = new OrganizationRepository(context2);
            var testOrg = new Organization("Test", MockedBusinessRegisterIdentifier.New(), _validAddress, _validDomain, "Test Comment");

            // Act
            var orgId = await orgRepository.AddOrUpdateAsync(testOrg);
            var newOrg = await orgRepository2.GetAsync(orgId.Value);

            // Assert
            Assert.NotNull(newOrg);
            Assert.NotEqual(Guid.Empty, newOrg.Id.Value);
            Assert.Equal(testOrg.Name, newOrg.Name);
            Assert.Equal(testOrg.Comment, newOrg.Comment);
            Assert.Equal(testOrg.Status, newOrg.Status);
        }

        [Fact]
        public async Task AddOrUpdateAsync_OneOrganizationWithAddress_CanReadBack()
        {
            // Arrange
            await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();
            await using var context2 = _fixture.DatabaseManager.CreateDbContext();

            var orgRepository = new OrganizationRepository(context);
            var orgRepository2 = new OrganizationRepository(context2);

            var testOrg = new Organization("Test", MockedBusinessRegisterIdentifier.New(), _validAddress, _validDomain, null);

            // Act
            var orgId = await orgRepository.AddOrUpdateAsync(testOrg);
            var newOrg = await orgRepository2.GetAsync(orgId.Value);

            // Assert
            Assert.NotNull(newOrg);
            Assert.NotEqual(Guid.Empty, newOrg.Id.Value);
            Assert.Equal(testOrg.Name, newOrg.Name);
            Assert.Equal(testOrg.Address.City, newOrg.Address.City);
        }

        [Fact]
        public async Task AddOrUpdateAsync_OneOrganizationWithBusinessRegisterIdentifier_CanReadBack()
        {
            // Arrange
            await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();
            await using var context2 = _fixture.DatabaseManager.CreateDbContext();

            var orgRepository = new OrganizationRepository(context);
            var orgRepository2 = new OrganizationRepository(context2);
            var testOrg = new Organization("Test", MockedBusinessRegisterIdentifier.New(), _validAddress, _validDomain, null);

            // Act
            var orgId = await orgRepository.AddOrUpdateAsync(testOrg);
            var newOrg = await orgRepository2.GetAsync(orgId.Value);

            // Assert
            Assert.NotNull(newOrg);
            Assert.NotEqual(Guid.Empty, newOrg.Id.Value);
            Assert.Equal(testOrg.Name, newOrg.Name);
            Assert.Equal(testOrg.BusinessRegisterIdentifier.Identifier, newOrg.BusinessRegisterIdentifier.Identifier);
        }

        [Fact]
        public async Task AddOrUpdateAsync_OrganizationNotExists_ReturnsNull()
        {
            // Arrange
            await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();
            var orgRepository = new OrganizationRepository(context);

            // Act
            var testOrg = await orgRepository
                .GetAsync(new OrganizationId(Guid.NewGuid()))
                ;

            // Assert
            Assert.Null(testOrg);
        }

        [Fact]
        public async Task AddOrUpdateAsync_OneOrganizationChanged_CanReadBack()
        {
            // Arrange
            await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();
            var orgRepository = new OrganizationRepository(context);
            var testOrg = new Organization("Test", MockedBusinessRegisterIdentifier.New(), _validAddress, _validDomain, null);

            // Act
            var orgId = await orgRepository.AddOrUpdateAsync(testOrg);
            var newOrg = await orgRepository.GetAsync(orgId.Value);

            newOrg = new Organization(
                newOrg!.Id,
                "NewName",
                newOrg.BusinessRegisterIdentifier,
                newOrg.Address,
                _validDomain,
                "Test Comment 2",
                OrganizationStatus.New);

            await orgRepository.AddOrUpdateAsync(newOrg);
            newOrg = await orgRepository.GetAsync(orgId.Value);

            // Assert
            Assert.NotNull(newOrg);
            Assert.NotEqual(Guid.Empty, newOrg.Id.Value);
            Assert.Equal("NewName", newOrg.Name);
            Assert.Equal("Test Comment 2", newOrg.Comment);
            Assert.Equal(OrganizationStatus.New, newOrg.Status);
        }

        [Fact]
        public async Task GetAsync_DifferentContexts_CanReadBack()
        {
            // Arrange
            await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();
            await using var context2 = _fixture.DatabaseManager.CreateDbContext();

            var orgRepository = new OrganizationRepository(context);
            var orgRepository2 = new OrganizationRepository(context2);

            var organization = new Organization("Test", MockedBusinessRegisterIdentifier.New(), _validAddress, _validDomain, null);

            // Act
            var orgId = await orgRepository.AddOrUpdateAsync(organization);
            organization = await orgRepository2.GetAsync(orgId.Value);

            // Assert
            Assert.NotNull(organization);
        }

        [Fact]
        public async Task GetAsync_All_ReturnsAllOrganizations()
        {
            // Arrange
            await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();
            await using var context2 = _fixture.DatabaseManager.CreateDbContext();

            var orgRepository = new OrganizationRepository(context);
            var orgRepository2 = new OrganizationRepository(context2);

            var organization = new Organization("Test", MockedBusinessRegisterIdentifier.New(), _validAddress, _validDomain, null);

            await orgRepository.AddOrUpdateAsync(organization);

            // Act
            var organizations = await orgRepository2.GetAsync();

            // Assert
            Assert.NotEmpty(organizations);
        }

        [Fact]
        public async Task GetAsync_GlobalLocationNumber_CanReadBack()
        {
            // Arrange
            await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();
            await using var context2 = _fixture.DatabaseManager.CreateDbContext();

            var orgRepository = new OrganizationRepository(context);
            var orgRepository2 = new OrganizationRepository(context2);

            var globalLocationNumber = new MockedGln();
            var organization = new Organization("Test", MockedBusinessRegisterIdentifier.New(), _validAddress, _validDomain, null);

            var organizationId = await orgRepository.AddOrUpdateAsync(organization);
            await _fixture.PrepareActorAsync(
                TestPreparationEntities.ValidOrganization,
                TestPreparationEntities.ValidActor.Patch(t =>
                {
                    t.ActorNumber = globalLocationNumber;
                    t.OrganizationId = organizationId.Value.Value;
                }));

            // Act
            var organizations = await orgRepository2.GetAsync(globalLocationNumber);

            // Assert
            Assert.NotNull(organizations);
        }
    }
}
