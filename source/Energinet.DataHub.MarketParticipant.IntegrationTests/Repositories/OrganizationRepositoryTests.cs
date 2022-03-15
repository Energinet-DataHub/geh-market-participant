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
using Energinet.DataHub.MarketParticipant.Domain.Model;
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
            var testOrg = new Organization("Test");

            // Act
            var orgId = await orgRepository.AddOrUpdateAsync(testOrg).ConfigureAwait(false);
            var newOrg = await orgRepository.GetAsync(orgId).ConfigureAwait(false);

            // Assert
            Assert.NotNull(newOrg);
            Assert.NotEqual(Guid.Empty, newOrg?.Id.Value);
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
            var orgRepository = new OrganizationRepository(context);
            var testOrg = new Organization("Test");

            // Act
            var orgId = await orgRepository.AddOrUpdateAsync(testOrg).ConfigureAwait(false);
            var newOrg = await orgRepository.GetAsync(orgId).ConfigureAwait(false);

            newOrg = new Organization(
                newOrg!.Id,
                "NewName",
                newOrg.Actors);

            await orgRepository.AddOrUpdateAsync(newOrg).ConfigureAwait(false);
            newOrg = await orgRepository.GetAsync(orgId).ConfigureAwait(false);

            // Assert
            Assert.NotNull(newOrg);
            Assert.NotEqual(Guid.Empty, newOrg?.Id.Value);
            Assert.Equal("NewName", newOrg?.Name);
        }

        [Fact]
        public async Task AddOrUpdateAsync_ActorAdded_CanReadBack()
        {
            // Arrange
            await using var host = await OrganizationHost.InitializeAsync().ConfigureAwait(false);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();
            var orgRepository = new OrganizationRepository(context);

            var initialActor = new Actor(
                Guid.Empty,
                new ExternalActorId(Guid.NewGuid()),
                new GlobalLocationNumber(Guid.NewGuid().ToString()),
                ActorStatus.New,
                new[]
                {
                    new GridArea(
                        new GridAreaId(Guid.Empty),
                        new GridAreaName("fake_value"),
                        new GridAreaCode("1234"))
                },
                Enumerable.Empty<MarketRole>(),
                Enumerable.Empty<MeteringPointType>());

            var organization = new Organization("Test");
            organization.Actors.Add(initialActor);

            var orgId = await orgRepository.AddOrUpdateAsync(organization).ConfigureAwait(false);
            organization = await orgRepository.GetAsync(orgId).ConfigureAwait(false);

            // Act
            var newActor = new Actor(new ExternalActorId(Guid.NewGuid()), new GlobalLocationNumber("fake_value"));
            organization!.Actors.Add(newActor);

            await orgRepository.AddOrUpdateAsync(organization).ConfigureAwait(false);
            organization = await orgRepository.GetAsync(orgId).ConfigureAwait(false);

            // Assert
            Assert.NotNull(organization);
            Assert.Equal(2, organization!.Actors.Count);
            Assert.Contains(organization.Actors, x => x.ExternalActorId == initialActor.ExternalActorId);
            Assert.Contains(organization.Actors, x => x.ExternalActorId == newActor.ExternalActorId);
        }

        [Fact]
        public async Task AddOrUpdateAsync_AddGridAreaToActor_CanReadBack()
        {
            // Arrange
            await using var host = await OrganizationHost.InitializeAsync().ConfigureAwait(false);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();
            var orgRepository = new OrganizationRepository(context);

            var organization = new Organization("Test");

            var expected = new GridArea(
                new GridAreaId(Guid.Empty),
                new GridAreaName("fake_value"),
                new GridAreaCode("1234"));

            organization.Actors.Add(new Actor(
                Guid.Empty,
                new ExternalActorId(Guid.NewGuid()),
                new GlobalLocationNumber("123"),
                ActorStatus.New,
                new[] { expected },
                Enumerable.Empty<MarketRole>(),
                Enumerable.Empty<MeteringPointType>()));

            // Act
            var orgId = await orgRepository.AddOrUpdateAsync(organization).ConfigureAwait(false);
            organization = await orgRepository.GetAsync(orgId).ConfigureAwait(false);
            var actual = organization!
                .Actors
                .Single()
                .Areas
                .Single();

            // Assert
            Assert.NotEqual(Guid.Empty, actual.Id.Value);
            Assert.Equal(expected.Name, actual.Name);
            Assert.Equal(expected.Code, actual.Code);
        }

        [Fact]
        public async Task AddOrUpdateAsync_MarketRoleAdded_CanReadBack()
        {
            // Arrange
            await using var host = await OrganizationHost.InitializeAsync().ConfigureAwait(false);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();
            var orgRepository = new OrganizationRepository(context);

            var organization = new Organization("Test");

            var balancePowerSupplierActor = new Actor(new ExternalActorId(Guid.NewGuid()), new GlobalLocationNumber("fake_value"));
            balancePowerSupplierActor.MarketRoles.Add(new MarketRole(EicFunction.BalancingServiceProvider));
            organization.Actors.Add(balancePowerSupplierActor);

            var orgId = await orgRepository.AddOrUpdateAsync(organization).ConfigureAwait(false);
            organization = await orgRepository.GetAsync(orgId).ConfigureAwait(false);

            // Act
            var meteringPointAdministratorActor = new Actor(new ExternalActorId(Guid.NewGuid()), new GlobalLocationNumber("fake_value"));
            meteringPointAdministratorActor.MarketRoles.Add(new MarketRole(EicFunction.MeteringPointAdministrator));
            organization!.Actors.Add(meteringPointAdministratorActor);

            await orgRepository.AddOrUpdateAsync(organization).ConfigureAwait(false);
            organization = await orgRepository.GetAsync(orgId).ConfigureAwait(false);

            // Assert
            Assert.NotNull(organization);
            Assert.Equal(2, organization!.Actors.Count);
            Assert.Contains(
                organization.Actors,
                x => x.MarketRoles.All(y => y.Function == EicFunction.BalancingServiceProvider));
            Assert.Contains(
                organization.Actors,
                x => x.MarketRoles.All(y => y.Function == EicFunction.MeteringPointAdministrator));
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

            var organization = new Organization("Test");
            var actorId = Guid.NewGuid();

            // Act
            organization.Actors.Add(new Actor(new ExternalActorId(actorId), new GlobalLocationNumber("fake_value")));
            var orgId = await orgRepository.AddOrUpdateAsync(organization).ConfigureAwait(false);
            organization = await orgRepository2.GetAsync(orgId).ConfigureAwait(false);

            // Assert
            Assert.NotNull(organization);
            Assert.Single(organization!.Actors);
            Assert.Contains(organization.Actors, x => x.ExternalActorId.Value == actorId);
        }

        [Fact]
        public async Task AddOrUpdateAsync_ActorWith1MeteringTypesAdded_CanReadBack()
        {
            // Arrange
            await using var host = await OrganizationHost.InitializeAsync().ConfigureAwait(false);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();
            await using var contextRead = _fixture.DatabaseManager.CreateDbContext();
            var orgRepository = new OrganizationRepository(context);
            var orgRepositoryRead = new OrganizationRepository(contextRead);

            var organization = new Organization("Test");

            var actorWithMeteringTypes = new Actor(new ExternalActorId(Guid.NewGuid()), new GlobalLocationNumber("fake_value"));
            actorWithMeteringTypes.MeteringPointTypes.Add(MeteringPointType.D03NotUsed);
            organization.Actors.Add(actorWithMeteringTypes);

            // Act
            var orgId = await orgRepository.AddOrUpdateAsync(organization).ConfigureAwait(false);
            organization = await orgRepositoryRead.GetAsync(orgId).ConfigureAwait(false);

            // Assert
            Assert.NotNull(organization);
            Assert.Contains(
                organization!.Actors.Single().MeteringPointTypes,
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

            var organization = new Organization("Test");

            var actorWithMeteringTypes = new Actor(new ExternalActorId(Guid.NewGuid()), new GlobalLocationNumber("fake_value"));
            actorWithMeteringTypes.MeteringPointTypes.Add(MeteringPointType.D03NotUsed);
            actorWithMeteringTypes.MeteringPointTypes.Add(MeteringPointType.D12TotalConsumption);
            organization.Actors.Add(actorWithMeteringTypes);

            // Act
            var orgId = await orgRepository.AddOrUpdateAsync(organization).ConfigureAwait(false);
            organization = await orgRepositoryRead.GetAsync(orgId).ConfigureAwait(false);

            // Assert
            Assert.NotNull(organization);
            Assert.Contains(
                organization!.Actors.Single().MeteringPointTypes,
                x => x.Equals(MeteringPointType.D03NotUsed));
            Assert.Contains(
                organization.Actors.Single().MeteringPointTypes,
                x => x.Equals(MeteringPointType.D12TotalConsumption));
        }

        [Fact]
        public async Task GetAsync_GlobalLocationNumber_CanReadBack()
        {
            // Arrange
            await using var host = await OrganizationHost.InitializeAsync().ConfigureAwait(false);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();
            await using var context2 = _fixture.DatabaseManager.CreateDbContext();

            var orgRepository = new OrganizationRepository(context);
            var orgRepository2 = new OrganizationRepository(context2);

            var globalLocationNumber = new GlobalLocationNumber("00000000");
            var organization = new Organization("Test");

            organization.Actors.Add(new Actor(new ExternalActorId(Guid.NewGuid()), globalLocationNumber));
            await orgRepository.AddOrUpdateAsync(organization).ConfigureAwait(false);

            // Act
            var organizations = await orgRepository2
                .GetAsync(globalLocationNumber)
                .ConfigureAwait(false);

            // Assert
            Assert.NotNull(organizations);
            var expected = organizations.Single();
            Assert.Equal(globalLocationNumber, expected.Actors.Single().Gln);
        }
    }
}
