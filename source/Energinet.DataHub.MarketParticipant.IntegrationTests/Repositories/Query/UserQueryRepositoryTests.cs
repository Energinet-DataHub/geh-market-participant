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
using Energinet.DataHub.Core.App.Common.Security;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories.Query;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories.Query;

[Collection("IntegrationTest")]
[IntegrationTest]
public sealed class UserQueryRepositoryTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public UserQueryRepositoryTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetActorsAsync_NoUser_ReturnsEmptyList()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var userRepository = new UserQueryRepository(context);

        var userExternalId = Guid.NewGuid();

        // Act
        var actorIds = (await userRepository
            .GetActorsAsync(new ExternalUserId(userExternalId)))
            .ToList();

        // Assert
        Assert.Empty(actorIds);
    }

    [Fact]
    public async Task GetActorsAsync_NoActor_ReturnsEmptyList()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var userRepository = new UserQueryRepository(context);

        var user = await _fixture.PrepareUserAsync();

        // Act
        var actorIds = (await userRepository
            .GetActorsAsync(new ExternalUserId(user.Id)))
            .ToList();

        // Assert
        Assert.Empty(actorIds);
    }

    [Fact]
    public async Task GetActorsAsync_WithActor_ReturnsCorrectId()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var userRepository = new UserQueryRepository(context);

        var user = await _fixture.PrepareUserAsync();
        var actor = await _fixture.PrepareActorAsync();
        var userRole = await _fixture.PrepareUserRoleAsync();
        await _fixture.AssignUserRoleAsync(user.Id, actor.Id, userRole.Id);

        // Act
        var actorIds = (await userRepository
            .GetActorsAsync(new ExternalUserId(user.ExternalId)))
            .ToList();

        // Assert
        Assert.NotEmpty(actorIds);
        Assert.Single(actorIds);
        Assert.Equal(actor.Id, actorIds.First());
    }

    [Fact]
    public async Task GetPermissionsAsync_UserDoesNotExist_ReturnsEmptyPermissions()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var userRepository = new UserQueryRepository(context);

        // Act
        var perms = await userRepository
            .GetPermissionsAsync(Guid.NewGuid(), new ExternalUserId(Guid.NewGuid()));

        // Assert
        Assert.Empty(perms);
    }

    [Fact]
    public async Task GetPermissionsAsync_UserExistWithNoPermissions_ReturnsZeroPermissions()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var userRepository = new UserQueryRepository(context);

        var user = await _fixture.PrepareUserAsync();

        // Act
        var perms = await userRepository
            .GetPermissionsAsync(Guid.NewGuid(), new ExternalUserId(user.ExternalId));

        // Assert
        Assert.Empty(perms);
    }

    [Fact]
    public async Task GetPermissionsAsync_UserExistWithPermissions_ReturnsPermissions()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var userRepository = new UserQueryRepository(context);

        var user = await _fixture.PrepareUserAsync();
        var actor = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor.Patch(t => t.Status = (int)ActorStatus.Active),
            TestPreparationEntities.ValidMarketRole.Patch(t => t.Function = EicFunction.BillingAgent));

        var userRole = await _fixture.PrepareUserRoleAsync(
            new[] { Permission.OrganizationView },
            EicFunction.BillingAgent);

        await _fixture.AssignUserRoleAsync(user.Id, actor.Id, userRole.Id);

        // Act
        var perms = (await userRepository
            .GetPermissionsAsync(actor.Id, new ExternalUserId(user.ExternalId)))
            .ToList();

        // Assert
        Assert.NotEmpty(perms);
        Assert.Equal(Permission.OrganizationView, perms.First());
    }

    [Fact]
    public async Task GetPermissionsAsync_ActorNotActive_ReturnsNoPermissions()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var userRepository = new UserQueryRepository(context);

        var user = await _fixture.PrepareUserAsync();
        var actor = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor.Patch(t => t.Status = (int)ActorStatus.Inactive),
            TestPreparationEntities.ValidMarketRole);

        var userRole = await _fixture.PrepareUserRoleAsync();

        await _fixture.AssignUserRoleAsync(user.Id, actor.Id, userRole.Id);

        // Act
        var perms = (await userRepository
            .GetPermissionsAsync(actor.Id, new ExternalUserId(user.ExternalId)))
            .ToList();

        // Assert
        Assert.Empty(perms);
    }

    [Fact]
    public async Task GetPermissionsAsync_ActorWrongEicFunction_ReturnsNoPermissions()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var userRepository = new UserQueryRepository(context);

        var user = await _fixture.PrepareUserAsync();
        var actor = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor.Patch(t => t.Status = (int)ActorStatus.Active),
            TestPreparationEntities.ValidMarketRole.Patch(t => t.Function = EicFunction.IndependentAggregator));

        var userRole = await _fixture.PrepareUserRoleAsync(
            new[] { Permission.OrganizationView },
            EicFunction.BillingAgent);

        await _fixture.AssignUserRoleAsync(user.Id, actor.Id, userRole.Id);

        // Act
        var perms = (await userRepository
            .GetPermissionsAsync(actor.Id, new ExternalUserId(user.ExternalId)))
            .ToList();

        // Assert
        Assert.Empty(perms);
    }

    [Fact]
    public async Task GetPermissionsAsync_DifferentActorInOrganization_ReturnsNoPermissions()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var userRepository = new UserQueryRepository(context);

        var actor = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor.Patch(t => t.Status = (int)ActorStatus.Active),
            TestPreparationEntities.ValidMarketRole.Patch(t => t.Function = EicFunction.BillingAgent));

        var doNotReturnActor = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor.Patch(t => t.Status = (int)ActorStatus.Active),
            TestPreparationEntities.ValidMarketRole.Patch(t => t.Function = EicFunction.BillingAgent));

        var user = await _fixture.PrepareUserAsync();
        var userRole = await _fixture.PrepareUserRoleAsync(
            new[] { Permission.OrganizationView },
            EicFunction.BillingAgent);

        await _fixture.AssignUserRoleAsync(user.Id, doNotReturnActor.Id, userRole.Id);

        // Act
        var perms = (await userRepository
            .GetPermissionsAsync(actor.Id, new ExternalUserId(user.ExternalId)))
            .ToList();

        // Assert
        Assert.Empty(perms);
    }

    [Fact]
    public async Task GetPermissionsAsync_UserExistWithPermissionsForMultipleActors_ReturnsCorrectPermissions()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var userRepository = new UserQueryRepository(context);

        var user = await _fixture.PrepareUserAsync();
        var actor1 = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor.Patch(t => t.Status = (int)ActorStatus.Active),
            TestPreparationEntities.ValidMarketRole.Patch(t => t.Function = EicFunction.BillingAgent));

        var actor2 = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor.Patch(t => t.Status = (int)ActorStatus.Active),
            TestPreparationEntities.ValidMarketRole.Patch(t => t.Function = EicFunction.GridAccessProvider));

        var userRole1 = await _fixture.PrepareUserRoleAsync(
            new[] { Permission.OrganizationView },
            EicFunction.BillingAgent);

        var userRole2 = await _fixture.PrepareUserRoleAsync(
            new[] { Permission.UsersView },
            EicFunction.GridAccessProvider);

        await _fixture.AssignUserRoleAsync(user.Id, actor1.Id, userRole1.Id);
        await _fixture.AssignUserRoleAsync(user.Id, actor2.Id, userRole2.Id);

        // Act
        var permsActor = (await userRepository
            .GetPermissionsAsync(actor1.Id, new ExternalUserId(user.ExternalId)))
            .ToList();

        var permsActor2 = (await userRepository
            .GetPermissionsAsync(actor2.Id, new ExternalUserId(user.ExternalId)))
            .ToList();

        // Assert
        Assert.NotEmpty(permsActor);
        Assert.NotEmpty(permsActor2);
        Assert.Single(permsActor);
        Assert.Single(permsActor2);
        Assert.Equal(Permission.OrganizationView, permsActor.First());
        Assert.Equal(Permission.UsersView, permsActor2.First());
    }

    [Fact]
    public async Task GetPermissionsAsync_UserExistWithMultiplePermissionsForActor_ReturnsCorrectPermissions()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var userRepository = new UserQueryRepository(context);

        var user = await _fixture.PrepareUserAsync();
        var actor = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor.Patch(t => t.Status = (int)ActorStatus.Active),
            TestPreparationEntities.ValidMarketRole.Patch(t => t.Function = EicFunction.DataHubAdministrator),
            TestPreparationEntities.ValidMarketRole.Patch(t => t.Function = EicFunction.GridAccessProvider));

        var userRole1 = await _fixture.PrepareUserRoleAsync(
            new[] { Permission.OrganizationManage, Permission.OrganizationView },
            EicFunction.DataHubAdministrator);

        var userRole2 = await _fixture.PrepareUserRoleAsync(
            new[] { Permission.UsersView },
            EicFunction.GridAccessProvider);

        await _fixture.AssignUserRoleAsync(user.Id, actor.Id, userRole1.Id);
        await _fixture.AssignUserRoleAsync(user.Id, actor.Id, userRole2.Id);

        // Act
        var permsActor = (await userRepository
            .GetPermissionsAsync(actor.Id, new ExternalUserId(user.ExternalId)))
            .ToList();

        // Assert
        Assert.NotEmpty(permsActor);
        Assert.Equal(3, permsActor.Count);
        Assert.Contains(Permission.OrganizationManage, permsActor);
        Assert.Contains(Permission.OrganizationView, permsActor);
        Assert.Contains(Permission.UsersView, permsActor);
    }

    [Fact]
    public async Task GetPermissionsAsync_UserExistWithPermissionsNotAllowedForEicFunction_ReturnsCorrectPermissions()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var userRepository = new UserQueryRepository(context);

        var user = await _fixture.PrepareUserAsync();
        var actor = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor.Patch(t => t.Status = (int)ActorStatus.Active),
            TestPreparationEntities.ValidMarketRole.Patch(t => t.Function = EicFunction.BalanceResponsibleParty));

        var userRole = await _fixture.PrepareUserRoleAsync(
            new[] { Permission.OrganizationManage, Permission.OrganizationView },
            EicFunction.BalanceResponsibleParty);

        await _fixture.AssignUserRoleAsync(user.Id, actor.Id, userRole.Id);

        // Act
        var perms = (await userRepository
            .GetPermissionsAsync(actor.Id, new ExternalUserId(user.ExternalId)))
            .ToList();

        // Assert
        Assert.NotEmpty(perms);
        Assert.Equal(Permission.OrganizationView, perms.Single());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task IsFas_Correct(bool isFas)
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var userRepository = new UserQueryRepository(context);

        var actor = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor.Patch(t =>
            {
                t.IsFas = isFas;
                t.Status = (int)ActorStatus.Active;
            }),
            TestPreparationEntities.ValidMarketRole);

        var user = await _fixture.PrepareUserAsync();
        var userRole = await _fixture.PrepareUserRoleAsync();
        await _fixture.AssignUserRoleAsync(user.Id, actor.Id, userRole.Id);

        // Act
        var actual = await userRepository.IsFasAsync(actor.Id, new ExternalUserId(user.ExternalId));

        // Assert
        Assert.Equal(isFas, actual);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task IsFas_DisabledActor_ReturnsFalse(bool isFas)
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var userRepository = new UserQueryRepository(context);

        var actor = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor.Patch(t =>
            {
                t.IsFas = isFas;
                t.Status = (int)ActorStatus.New;
            }),
            TestPreparationEntities.ValidMarketRole);

        var user = await _fixture.PrepareUserAsync();
        var userRole = await _fixture.PrepareUserRoleAsync();
        await _fixture.AssignUserRoleAsync(user.Id, actor.Id, userRole.Id);

        // Act
        var actual = await userRepository
            .IsFasAsync(actor.Id, new ExternalUserId(user.ExternalId));

        // Assert
        Assert.False(actual);
    }
}
