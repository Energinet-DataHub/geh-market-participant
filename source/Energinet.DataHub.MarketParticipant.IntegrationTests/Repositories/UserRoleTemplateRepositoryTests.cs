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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Categories;
namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories
{
    [Collection("IntegrationTest")]
    [IntegrationTest]
    public sealed class UserRoleTemplateRepositoryTests
    {
        private readonly MarketParticipantDatabaseFixture _fixture;
        public UserRoleTemplateRepositoryTests(MarketParticipantDatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task AddOrUpdateAsync_UserRoleTemplateAdded_CanReadBack()
        {
            // Arrange
            await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();
            await using var context2 = _fixture.DatabaseManager.CreateDbContext();
            var userRoleTemplateRepository = new UserRoleTemplateRepository(context);
            var userRoleTemplateRepository2 = new UserRoleTemplateRepository(context2);
            var testRole = new UserRoleTemplate("Test", new List<Permission>());

            // Act
            var testRoleId = await userRoleTemplateRepository.AddOrUpdateAsync(testRole);
            var newRole = await userRoleTemplateRepository2.GetAsync(testRoleId);

            // Assert
            Assert.NotNull(newRole);
            Assert.NotEqual(Guid.Empty, newRole?.Id);
            Assert.Equal(testRole.Name, newRole?.Name);
            Assert.Equal(0, newRole?.Permissions.Count());
        }

        [Fact]
        public async Task AddOrUpdateAsync_UserRoleTemplateWithPermissionsAdded_CanReadBack()
        {
            // Arrange
            await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();
            await using var context2 = _fixture.DatabaseManager.CreateDbContext();
            var userRoleTemplateRepository = new UserRoleTemplateRepository(context);
            var userRoleTemplateRepository2 = new UserRoleTemplateRepository(context2);
            context.Permissions.Add(new PermissionEntity("perm1", "Test 1"));
            context.Permissions.Add(new PermissionEntity("perm2", "Test 2"));
            await context.SaveChangesAsync();
            var testRole = new UserRoleTemplate("Test", new List<Permission>() { new("perm1", "perm1 desc"), new("perm2", "perm2 desc") });

            // Act
            var testRoleId = await userRoleTemplateRepository.AddOrUpdateAsync(testRole);
            var newRole = await userRoleTemplateRepository2.GetAsync(testRoleId);

            // Assert
            Assert.NotNull(newRole);
            Assert.NotEqual(Guid.Empty, newRole?.Id);
            Assert.Equal(testRole.Name, newRole?.Name);
            Assert.Equal(2, newRole?.Permissions.Count());
        }

        [Fact]
        public async Task AddOrUpdateAsync_UserRoleTemplateGetToMarketWithPermissionsAdded_CanRead()
        {
            // Arrange
            await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();
            await using var context2 = _fixture.DatabaseManager.CreateDbContext();
            var userRoleTemplateRepository = new UserRoleTemplateRepository(context);
            var userRoleTemplateRepository2 = new UserRoleTemplateRepository(context2);
            context.Permissions.Add(new PermissionEntity("perm1", "Test 1"));
            context.Permissions.Add(new PermissionEntity("perm2", "Test 2"));

            var testRole = new UserRoleTemplate("ATest", new List<Permission>() { new("perm1", "perm1 desc"), new("perm2", "perm2 desc") });
            var testRole2 = new UserRoleTemplate("BTest", new List<Permission>() { new("perm1", "perm1 desc"), new("perm2", "perm2 desc") });
            var testRole3 = new UserRoleTemplate("CTest", new List<Permission>() { new("perm1", "perm1 desc"), new("perm2", "perm2 desc") });

            // Act
            var testRoleId = await userRoleTemplateRepository.AddOrUpdateAsync(testRole);
            var testRoleId2 = await userRoleTemplateRepository.AddOrUpdateAsync(testRole2);
            var testRoleId3 = await userRoleTemplateRepository.AddOrUpdateAsync(testRole3);
            context.MarketRoleToUserRoleTemplate.Add(new MarketRoleToUserRoleTemplateEntity() { Function = EicFunction.EnergySupplier, UserRoleTemplateId = testRoleId });
            context.MarketRoleToUserRoleTemplate.Add(new MarketRoleToUserRoleTemplateEntity() { Function = EicFunction.EnergySupplier, UserRoleTemplateId = testRoleId2 });
            context.MarketRoleToUserRoleTemplate.Add(new MarketRoleToUserRoleTemplateEntity() { Function = EicFunction.BillingAgent, UserRoleTemplateId = testRoleId3 });
            await context.SaveChangesAsync();
            var roles = (await userRoleTemplateRepository2.GetForMarketAsync(EicFunction.EnergySupplier)).ToList();
            var roles2 = (await userRoleTemplateRepository2.GetForMarketAsync(EicFunction.BillingAgent)).ToList();

            // Assert
            Assert.NotNull(roles);
            Assert.Equal(2, roles.Count);
            Assert.Single(roles2);
            Assert.NotEqual(Guid.Empty, roles.First().Id);
            Assert.NotEqual(Guid.Empty, roles.Skip(1).First().Id);
            Assert.NotEqual(Guid.Empty, roles2.First().Id);
            Assert.Equal(testRoleId, roles.First().Id);
            Assert.Equal(testRoleId2, roles.Skip(1).First().Id);
            Assert.Equal(testRoleId3, roles2.First().Id);
            Assert.Equal(2, roles.First().Permissions.Count());
            Assert.Equal(2, roles.Skip(1).First().Permissions.Count());
            Assert.Equal(2, roles2.First().Permissions.Count());
            Assert.Equal("ATest", roles.First().Name);
            Assert.Equal("BTest", roles.Skip(1).First().Name);
            Assert.Equal("CTest", roles2.First().Name);
        }

        // [Fact]
        // public async Task AddOrUpdateAsync_OrganizationNotExists_ReturnsNull()
        // {
        //     // Arrange
        //     await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        //     await using var scope = host.BeginScope();
        //     await using var context = _fixture.DatabaseManager.CreateDbContext();
        //     var orgRepository = new OrganizationRepository(context);
        //
        //     // Act
        //     var testOrg = await orgRepository
        //         .GetAsync(new OrganizationId(Guid.NewGuid()))
        //         ;
        //
        //     // Assert
        //     Assert.Null(testOrg);
        // }
        //
        // [Fact]
        // public async Task AddOrUpdateAsync_OneOrganizationChanged_CanReadBack()
        // {
        //     // Arrange
        //     await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        //     await using var scope = host.BeginScope();
        //     await using var context = _fixture.DatabaseManager.CreateDbContext();
        //     var orgRepository = new OrganizationRepository(context);
        //     var testOrg = new Organization("Test", MockedBusinessRegisterIdentifier.New(), _validAddress);
        //
        //     // Act
        //     var orgId = await orgRepository.AddOrUpdateAsync(testOrg);
        //     var newOrg = await orgRepository.GetAsync(orgId);
        //
        //     newOrg = new Organization(
        //         newOrg!.Id,
        //         "NewName",
        //         newOrg.Actors,
        //         newOrg.BusinessRegisterIdentifier,
        //         newOrg.Address,
        //         "Test Comment 2",
        //         OrganizationStatus.New);
        //
        //     await orgRepository.AddOrUpdateAsync(newOrg);
        //     newOrg = await orgRepository.GetAsync(orgId);
        //
        //     // Assert
        //     Assert.NotNull(newOrg);
        //     Assert.NotEqual(Guid.Empty, newOrg?.Id.Value);
        //     Assert.Equal("NewName", newOrg?.Name);
        //     Assert.Equal("Test Comment 2", newOrg?.Comment);
        //     Assert.Equal(OrganizationStatus.New, newOrg?.Status);
        // }
        //
        // [Fact]
        // public async Task GetAsync_DifferentContexts_CanReadBack()
        // {
        //     // Arrange
        //     await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        //     await using var scope = host.BeginScope();
        //     await using var context = _fixture.DatabaseManager.CreateDbContext();
        //     await using var context2 = _fixture.DatabaseManager.CreateDbContext();
        //
        //     var orgRepository = new OrganizationRepository(context);
        //     var orgRepository2 = new OrganizationRepository(context2);
        //
        //     var organization = new Organization("Test", MockedBusinessRegisterIdentifier.New(), _validAddress);
        //     var gln = new MockedGln();
        //
        //     // Act
        //     organization.Actors.Add(new Actor(gln));
        //     var orgId = await orgRepository.AddOrUpdateAsync(organization);
        //     organization = await orgRepository2.GetAsync(orgId);
        //
        //     // Assert
        //     Assert.NotNull(organization);
        //     Assert.Single(organization!.Actors);
        //     Assert.Contains(organization.Actors, x => x.ActorNumber == gln);
        // }
        //
        // [Fact]
        // public async Task GetAsync_All_ReturnsAllOrganizations()
        // {
        //     // Arrange
        //     await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        //     await using var scope = host.BeginScope();
        //     await using var context = _fixture.DatabaseManager.CreateDbContext();
        //     await using var context2 = _fixture.DatabaseManager.CreateDbContext();
        //
        //     var orgRepository = new OrganizationRepository(context);
        //     var orgRepository2 = new OrganizationRepository(context2);
        //
        //     var globalLocationNumber = new MockedGln();
        //     var organization = new Organization("Test", MockedBusinessRegisterIdentifier.New(), _validAddress);
        //
        //     organization.Actors.Add(new Actor(globalLocationNumber));
        //     await orgRepository.AddOrUpdateAsync(organization);
        //
        //     // Act
        //     var organizations = await orgRepository2
        //         .GetAsync()
        //         ;
        //
        //     // Assert
        //     Assert.NotEmpty(organizations);
        // }
    }
}
