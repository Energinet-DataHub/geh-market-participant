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
    public sealed class UserRoleTemplateRepositoryTests : IAsyncLifetime
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
            var testRole = new UserRoleTemplate("Test", new List<UserRolePermission>());

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
            var testRole = new UserRoleTemplate("Test", new List<UserRolePermission>() { new() { PermissionId = Core.App.Common.Security.Permission.OrganizationManage.ToString() }, new() { PermissionId = Core.App.Common.Security.Permission.GridAreasManage.ToString() } });

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
            var testRole = new UserRoleTemplate("ATest", new List<UserRolePermission>() { new() { PermissionId = Core.App.Common.Security.Permission.OrganizationManage.ToString() }, new() { PermissionId = Core.App.Common.Security.Permission.GridAreasManage.ToString() } });
            var testRole2 = new UserRoleTemplate("BTest", new List<UserRolePermission>() { new() { PermissionId = Core.App.Common.Security.Permission.OrganizationManage.ToString() }, new() { PermissionId = Core.App.Common.Security.Permission.GridAreasManage.ToString() } });
            var testRole3 = new UserRoleTemplate("CTest", new List<UserRolePermission>() { new() { PermissionId = Core.App.Common.Security.Permission.OrganizationManage.ToString() }, new() { PermissionId = Core.App.Common.Security.Permission.GridAreasManage.ToString() } });

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

        public async Task InitializeAsync()
        {
            // Permissions are needed for all user/userRole tests, and they are the same, so are initialized here
            await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();

            context.Permissions.Add(new PermissionEntity(Core.App.Common.Security.Permission.OrganizationManage.ToString(), "Test 1"));
            context.Permissions.Add(new PermissionEntity(Core.App.Common.Security.Permission.GridAreasManage.ToString(), "Test 2"));
            await context.SaveChangesAsync();
        }

        public async Task DisposeAsync()
        {
            // Permissions are needed for all user/userRole tests, and they are the same, so are initialized here
            await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();

            context.Permissions.Remove(new PermissionEntity(Core.App.Common.Security.Permission.OrganizationManage.ToString(), "Test 1"));
            context.Permissions.Remove(new PermissionEntity(Core.App.Common.Security.Permission.GridAreasManage.ToString(), "Test 2"));
            await context.SaveChangesAsync();
        }
    }
}
