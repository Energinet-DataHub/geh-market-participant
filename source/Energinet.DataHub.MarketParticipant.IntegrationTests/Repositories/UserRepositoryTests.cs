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
using Energinet.DataHub.Core.App.Common.Security;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories
{
    [Collection("IntegrationTest")]
    [IntegrationTest]
    public sealed class UserRepositoryTests
    {
        private readonly MarketParticipantDatabaseFixture _fixture;

        public UserRepositoryTests(MarketParticipantDatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task GetPermissionsAsync_UserDoesntExist_ReturnsEmptyPermissions()
        {
            // Arrange
            await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();
            var userRepository = new UserRepository(context);

            // Act
            var perms = await userRepository
                .GetPermissionsAsync(Guid.NewGuid(), Guid.NewGuid());

            // Assert
            Assert.Empty(perms);
        }

        [Fact]
        public async Task GetPermissionsAsync_UserExistWithNoPermissions_ReturnsZeroPermissions()
        {
            // Arrange
            await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();
            var userRepository = new UserRepository(context);

            var userExternalId = Guid.NewGuid();
            var userEntity = new UserEntity() { ExternalId = userExternalId, Name = "Test User", RoleAssigments = { } };
            await context.Users.AddAsync(userEntity);
            await context.SaveChangesAsync();

            // Act
            var perms = await userRepository
                .GetPermissionsAsync(Guid.NewGuid(), userExternalId);

            // Assert
            Assert.Empty(perms);
        }

        [Fact]
        public async Task GetPermissionsAsync_UserExistWithPermissions_ReturnsPermissions()
        {
            // Arrange
            await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
            await using var scope = host.BeginScope();
            await using var context = _fixture.DatabaseManager.CreateDbContext();
            var userRepository = new UserRepository(context);

            var userExternalId = Guid.NewGuid();
            var externalActorId = Guid.NewGuid();
            var actorEntity = new ActorEntity()
            {
                ActorId = externalActorId,
                Name = "Test Actor",
                ActorNumber = new MockedGln(),
                Status = (int)ActorStatus.Active
            };
            var orgEntity = new OrganizationEntity()
            {
                Actors = { actorEntity },
                Address = new AddressEntity()
                {
                    City = "test city",
                    Country = "Denmark",
                    Number = "1",
                    StreetName = "Teststreet",
                    ZipCode = "1234"
                },
                Name = "Test Org",
                BusinessRegisterIdentifier = "12345678"
            };

            await context.Organizations.AddAsync(orgEntity);
            await context.SaveChangesAsync();
            var userRoleTemplate = new UserRoleTemplateEntity()
            {
                Name = "Test Template",
                Permissions =
                {
                    new UserRoleTemplatePermissionEntity() { Permission = Permission.OrganizationManage }
                },
                EicFunctions =
                {
                    new UserRoleTemplateEicFunctionEntity() { EicFunction = EicFunction.BillingAgent }
                },
            };
            await context.Entry(actorEntity).ReloadAsync();
            var roleAssignment = new UserRoleAssignmentEntity()
            {
                ActorId = actorEntity.Id, UserRoleTemplate = userRoleTemplate,
            };
            var userEntity = new UserEntity() { ExternalId = userExternalId, Name = "Test User", RoleAssigments = { roleAssignment } };
            await context.Users.AddAsync(userEntity);
            await context.SaveChangesAsync();

            // Act
            var perms = await userRepository
                .GetPermissionsAsync(externalActorId, userExternalId);

            // Assert
            Assert.NotEmpty(perms);
        }
    }
}
