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
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories;

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
    public async Task GetUserAsyncExternalId_UserDoesntExist_ReturnsNull()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var userIdentityRepository = new Mock<IUserIdentityRepository>().Object;
        var userRepository = new UserRepository(context, userIdentityRepository);

        // Act
        var user = await userRepository.GetAsync(new ExternalUserId(Guid.NewGuid()));

        // Assert
        Assert.Null(user);
    }

    [Fact]
    public async Task GetUserAsyncExternalId_SimpleUserExist_CanReadBack()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var userIdentityRepository = new Mock<IUserIdentityRepository>().Object;
        var userRepository = new UserRepository(context, userIdentityRepository);

        var user = await _fixture.PrepareUserAsync();

        // Act
        var actual = await userRepository.GetAsync(new ExternalUserId(user.ExternalId));

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(user.ExternalId, actual.ExternalId.Value);
        Assert.NotEqual(Guid.Empty, actual.Id.Value);
    }

    [Fact]
    public async Task GetUserAsyncExternalId_UserExist_CanReadBack()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var userIdentityRepository = new Mock<IUserIdentityRepository>().Object;
        var userRepository = new UserRepository(context, userIdentityRepository);

        var actor = await _fixture.PrepareActorAsync();
        var user = await _fixture.PrepareUserAsync();
        var userRole = await _fixture.PrepareUserRoleAsync();
        await _fixture.AssignUserRoleAsync(user.Id, actor.Id, userRole.Id);

        // Act
        var actual = await userRepository.GetAsync(new ExternalUserId(user.ExternalId));

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(user.ExternalId, actual.ExternalId.Value);
        Assert.Equal(user.Id, actual.Id.Value);
        Assert.Single(actual.RoleAssignments);
        Assert.Equal(userRole.Id, actual.RoleAssignments.First().UserRoleId.Value);
    }

    [Fact]
    public async Task GetUserAsync_UserDoesntExist_ReturnsNull()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var userIdentityRepository = new Mock<IUserIdentityRepository>().Object;
        var userRepository = new UserRepository(context, userIdentityRepository);

        // Act
        var user = await userRepository.GetAsync(new UserId(Guid.NewGuid()));

        // Assert
        Assert.Null(user);
    }

    [Fact]
    public async Task GetUserAsync_SimpleUserExist_CanReadBack()
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var userIdentityRepository = new Mock<IUserIdentityRepository>().Object;
        var userRepository = new UserRepository(context, userIdentityRepository);

        var user = await _fixture.PrepareUserAsync();

        // Act
        var actual = await userRepository.GetAsync(new UserId(user.Id));

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(user.Id, actual.Id.Value);
    }
}
