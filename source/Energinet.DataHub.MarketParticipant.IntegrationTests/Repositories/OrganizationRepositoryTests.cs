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
using Energinet.DataHub.MarketParticipant.Domain.Model.Roles;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;
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

        public OrganizationRepositoryTests(MarketParticipantDatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task AddOrUpdateAsync_OneOrganization_CanReadBack()
        {
            // Arrange
            await using var host = await OrganizationHost.InitializeAsync().ConfigureAwait(false);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();
            var orgRepository = new OrganizationRepository(context);
            var testOrg = new Organization(
                Guid.NewGuid(),
                new GlobalLocationNumber(Guid.NewGuid().ToString()),
                "Test");

            // Act
            var orgId = await orgRepository.AddOrUpdateAsync(testOrg).ConfigureAwait(false);
            var newOrg = await orgRepository.GetAsync(orgId).ConfigureAwait(false);

            // Assert
            Assert.NotNull(newOrg);
            Assert.NotEqual(Guid.Empty, newOrg?.Id.Value);
            Assert.Equal(testOrg.Gln.Value, newOrg?.Gln.Value);
            Assert.Equal(testOrg.Name, newOrg?.Name);
        }

        [Fact]
        public async Task AddOrUpdateAsync_OrganizationNotExists_ReturnsNull()
        {
            // Arrange
            await using var host = await OrganizationHost.InitializeAsync().ConfigureAwait(false);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();
            var orgRepository = new OrganizationRepository(context);

            // Act
            var testOrg = await orgRepository
                .GetAsync(new OrganizationId(Guid.NewGuid()))
                .ConfigureAwait(false);

            // Assert
            Assert.Null(testOrg);
        }

        [Fact]
        public async Task AddOrUpdateAsync_OneOrganizationChanged_CanReadBack()
        {
            // Arrange
            await using var host = await OrganizationHost.InitializeAsync().ConfigureAwait(false);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();
            var gln = new GlobalLocationNumber(Guid.NewGuid().ToString());
            var orgRepository = new OrganizationRepository(context);
            var testOrg = new Organization(
                Guid.NewGuid(),
                gln,
                "Test");

            // Act
            var orgId = await orgRepository.AddOrUpdateAsync(testOrg).ConfigureAwait(false);
            var newOrg = await orgRepository.GetAsync(orgId).ConfigureAwait(false);

            newOrg = new Organization(
                newOrg!.Id,
                newOrg.ActorId,
                gln,
                "NewName",
                newOrg.Roles);

            await orgRepository.AddOrUpdateAsync(newOrg).ConfigureAwait(false);
            newOrg = await orgRepository.GetAsync(orgId).ConfigureAwait(false);

            // Assert
            Assert.NotNull(newOrg);
            Assert.NotEqual(Guid.Empty, newOrg?.Id.Value);
            Assert.Equal(gln.Value, newOrg?.Gln.Value);
            Assert.Equal("NewName", newOrg?.Name);
        }

        [Fact]
        public async Task AddOrUpdateAsync_OrganizationRoleAdded_CanReadBack()
        {
            // Arrange
            await using var host = await OrganizationHost.InitializeAsync().ConfigureAwait(false);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();
            var orgRepository = new OrganizationRepository(context);

            var organization = new Organization(
                Guid.NewGuid(),
                new GlobalLocationNumber(Guid.NewGuid().ToString()),
                "Test");

            organization.AddRole(new BalancePowerSupplierRole(
                Guid.Empty,
                RoleStatus.New,
                new GridArea(
                    new GridAreaId(Guid.Empty),
                    new GridAreaName("fake_value"),
                    new GridAreaCode("1234")),
                new List<MarketRole>(),
                new List<MeteringPointType>()));

            var orgId = await orgRepository.AddOrUpdateAsync(organization).ConfigureAwait(false);
            organization = await orgRepository.GetAsync(orgId).ConfigureAwait(false);

            // Act
            organization!.AddRole(new DanishEnergyAgencyRole());

            await orgRepository.AddOrUpdateAsync(organization).ConfigureAwait(false);
            organization = await orgRepository.GetAsync(orgId).ConfigureAwait(false);

            // Assert
            Assert.NotNull(organization);
            Assert.Equal(2, organization!.Roles.Count());
            Assert.Contains(organization.Roles, x => x is BalancePowerSupplierRole);
            Assert.Contains(organization.Roles, x => x is DanishEnergyAgencyRole);
        }

        [Fact]
        public async Task AddOrUpdateAsync_AddGridAreaToOrganizationRole_CanReadBack()
        {
            // Arrange
            await using var host = await OrganizationHost.InitializeAsync().ConfigureAwait(false);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();
            var orgRepository = new OrganizationRepository(context);

            var organization = new Organization(
                Guid.Empty,
                new GlobalLocationNumber("123"),
                "Test");

            organization.AddRole(new BalancePowerSupplierRole(
                Guid.Empty,
                RoleStatus.New,
                new GridArea(
                    new GridAreaId(Guid.Empty),
                    new GridAreaName("fake_value"),
                    new GridAreaCode("1234")),
                new List<MarketRole>(),
                new List<MeteringPointType>()));

            // Act
            var orgId = await orgRepository.AddOrUpdateAsync(organization).ConfigureAwait(false);
            organization = await orgRepository.GetAsync(orgId).ConfigureAwait(false);

            // Assert
            Assert.Equal("fake_value", organization?.Roles.First().Area?.Name.Value);
            Assert.NotEqual(Guid.Empty, organization?.Roles.First().Area?.Id.Value);
            Assert.Equal("1234", organization?.Roles.First().Area?.Code.Value);
        }

        [Fact]
        public async Task AddOrUpdateAsync_MarketRoleAdded_CanReadBack()
        {
            // Arrange
            await using var host = await OrganizationHost.InitializeAsync().ConfigureAwait(false);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();
            var orgRepository = new OrganizationRepository(context);

            var organization = new Organization(
                Guid.NewGuid(),
                new GlobalLocationNumber(Guid.NewGuid().ToString()),
                "Test");

            organization.AddRole(new BalancePowerSupplierRole
            {
                MarketRoles = { new MarketRole(EicFunction.BalancingServiceProvider) }
            });

            var orgId = await orgRepository.AddOrUpdateAsync(organization).ConfigureAwait(false);
            organization = await orgRepository.GetAsync(orgId).ConfigureAwait(false);

            // Act
            organization!.AddRole(new DanishEnergyAgencyRole
            {
                MarketRoles = { new MarketRole(EicFunction.SystemOperator) }
            });

            await orgRepository.AddOrUpdateAsync(organization).ConfigureAwait(false);
            organization = await orgRepository.GetAsync(orgId).ConfigureAwait(false);

            // Assert
            Assert.NotNull(organization);
            Assert.Equal(2, organization!.Roles.Count());
            Assert.Contains(
                organization.Roles,
                x => x is BalancePowerSupplierRole role &&
                     role.MarketRoles.All(y => y.Function == EicFunction.BalancingServiceProvider));
            Assert.Contains(
                organization.Roles,
                x => x is DanishEnergyAgencyRole role &&
                     role.MarketRoles.All(y => y.Function == EicFunction.SystemOperator));
        }

        [Fact]
        public async Task GetAsync_DifferentContexts_CanReadBack()
        {
            // Arrange
            await using var host = await OrganizationHost.InitializeAsync().ConfigureAwait(false);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();
            await using var context2 = _fixture.DatabaseManager.CreateDbContext();

            var orgRepository = new OrganizationRepository(context);
            var orgRepository2 = new OrganizationRepository(context2);

            var organization = new Organization(
                Guid.NewGuid(),
                new GlobalLocationNumber(Guid.NewGuid().ToString()),
                "Test");

            // Act
            organization.AddRole(new BalancePowerSupplierRole());
            var orgId = await orgRepository.AddOrUpdateAsync(organization).ConfigureAwait(false);
            organization = await orgRepository2.GetAsync(orgId).ConfigureAwait(false);

            // Assert
            Assert.NotNull(organization);
            Assert.Single(organization!.Roles);
            Assert.Contains(organization.Roles, x => x is BalancePowerSupplierRole);
        }

        [Fact]
        public async Task AddOrUpdateAsync_OrganizationRoleWith1MeteringTypesAdded_CanReadBack()
        {
            // Arrange
            await using var host = await OrganizationHost.InitializeAsync().ConfigureAwait(false);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();
            await using var contextRead = _fixture.DatabaseManager.CreateDbContext();
            var orgRepository = new OrganizationRepository(context);
            var orgRepositoryRead = new OrganizationRepository(contextRead);

            var organization = new Organization(
                Guid.NewGuid(),
                new GlobalLocationNumber(Guid.NewGuid().ToString()),
                "Test");

            var roleWithMeteringTypes = new MeteringPointAdministratorRole();
            roleWithMeteringTypes.MeteringPointTypes.Add(MeteringPointType.D03NotUsed);
            organization.AddRole(roleWithMeteringTypes);

            // Act
            var orgId = await orgRepository.AddOrUpdateAsync(organization).ConfigureAwait(false);
            organization = await orgRepositoryRead.GetAsync(orgId).ConfigureAwait(false);

            // Assert
            Assert.NotNull(organization);
            Assert.Single(organization!.Roles);
            Assert.Contains(organization.Roles, x => x is MeteringPointAdministratorRole);
            Assert.Contains(
                organization.Roles.First().MeteringPointTypes,
                x => x.Equals(MeteringPointType.D03NotUsed));
        }

        [Fact]
        public async Task AddOrUpdateAsync_OrganizationRoleWith2MeteringTypesAdded_CanReadBack()
        {
            // Arrange
            await using var host = await OrganizationHost.InitializeAsync().ConfigureAwait(false);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();
            await using var contextRead = _fixture.DatabaseManager.CreateDbContext();
            var orgRepository = new OrganizationRepository(context);
            var orgRepositoryRead = new OrganizationRepository(contextRead);

            var organization = new Organization(
                Guid.NewGuid(),
                new GlobalLocationNumber(Guid.NewGuid().ToString()),
                "Test");

            var roleWithMeteringTypes = new MeteringPointAdministratorRole();
            roleWithMeteringTypes.MeteringPointTypes.Add(MeteringPointType.D03NotUsed);
            roleWithMeteringTypes.MeteringPointTypes.Add(MeteringPointType.D12TotalConsumption);
            organization.AddRole(roleWithMeteringTypes);

            // Act
            var orgId = await orgRepository.AddOrUpdateAsync(organization).ConfigureAwait(false);
            organization = await orgRepositoryRead.GetAsync(orgId).ConfigureAwait(false);

            // Assert
            Assert.NotNull(organization);
            Assert.Single(organization!.Roles);
            Assert.Equal(2, organization!.Roles.First().MeteringPointTypes.Count);
            Assert.Contains(organization.Roles, x => x is MeteringPointAdministratorRole);
            Assert.Contains(
                organization.Roles.First().MeteringPointTypes,
                x => x.Equals(MeteringPointType.D03NotUsed));
            Assert.Contains(
                organization.Roles.First().MeteringPointTypes,
                x => x.Equals(MeteringPointType.D12TotalConsumption));
        }
    }
}
