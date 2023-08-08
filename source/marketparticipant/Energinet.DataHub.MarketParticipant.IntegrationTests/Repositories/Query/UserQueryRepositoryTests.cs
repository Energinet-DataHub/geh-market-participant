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
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories.Query;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories.Query;

[Collection(nameof(IntegrationTestCollectionFixture))]
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
        var userRepository = new UserQueryRepository(context, scope.GetInstance<IPermissionRepository>());

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
        var userRepository = new UserQueryRepository(context, scope.GetInstance<IPermissionRepository>());

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
        var userRepository = new UserQueryRepository(context, scope.GetInstance<IPermissionRepository>());

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
        Assert.Equal(actor.Id, actorIds.First().Value);
    }

    [Fact]
    public async Task GetPermissionsAsync_UserDoesNotExist_ReturnsEmptyPermissions()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var userRepository = new UserQueryRepository(context, scope.GetInstance<IPermissionRepository>());

        // Act
        var perms = await userRepository
            .GetPermissionsAsync(new ActorId(Guid.NewGuid()), new ExternalUserId(Guid.NewGuid()));

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
        var userRepository = new UserQueryRepository(context, scope.GetInstance<IPermissionRepository>());

        var user = await _fixture.PrepareUserAsync();

        // Act
        var perms = await userRepository
            .GetPermissionsAsync(new ActorId(Guid.NewGuid()), new ExternalUserId(user.ExternalId));

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
        var userRepository = new UserQueryRepository(context, scope.GetInstance<IPermissionRepository>());

        var user = await _fixture.PrepareUserAsync();
        var actor = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor.Patch(t => t.Status = ActorStatus.Active),
            TestPreparationEntities.ValidMarketRole.Patch(t => t.Function = EicFunction.BillingAgent));

        var userRole = await _fixture.PrepareUserRoleAsync(
            new[] { PermissionId.OrganizationsView },
            EicFunction.BillingAgent);

        await _fixture.AssignUserRoleAsync(user.Id, actor.Id, userRole.Id);

        // Act
        var perms = (await userRepository
            .GetPermissionsAsync(new ActorId(actor.Id), new ExternalUserId(user.ExternalId)))
            .ToList();

        // Assert
        Assert.NotEmpty(perms);
        Assert.Equal(PermissionId.OrganizationsView, perms.First().Id);
    }

    [Fact]
    public async Task GetPermissionsAsync_ActorNotActive_ReturnsNoPermissions()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var userRepository = new UserQueryRepository(context, scope.GetInstance<IPermissionRepository>());

        var user = await _fixture.PrepareUserAsync();
        var actor = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor.Patch(t => t.Status = ActorStatus.Inactive),
            TestPreparationEntities.ValidMarketRole);

        var userRole = await _fixture.PrepareUserRoleAsync();

        await _fixture.AssignUserRoleAsync(user.Id, actor.Id, userRole.Id);

        // Act
        var perms = (await userRepository
            .GetPermissionsAsync(new ActorId(actor.Id), new ExternalUserId(user.ExternalId)))
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
        var userRepository = new UserQueryRepository(context, scope.GetInstance<IPermissionRepository>());

        var user = await _fixture.PrepareUserAsync();
        var actor = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor.Patch(t => t.Status = ActorStatus.Active),
            TestPreparationEntities.ValidMarketRole.Patch(t => t.Function = EicFunction.IndependentAggregator));

        var userRole = await _fixture.PrepareUserRoleAsync(
            new[] { PermissionId.OrganizationsView },
            EicFunction.BillingAgent);

        await _fixture.AssignUserRoleAsync(user.Id, actor.Id, userRole.Id);

        // Act
        var perms = (await userRepository
            .GetPermissionsAsync(new ActorId(actor.Id), new ExternalUserId(user.ExternalId)))
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
        var userRepository = new UserQueryRepository(context, scope.GetInstance<IPermissionRepository>());

        var actor = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor.Patch(t => t.Status = ActorStatus.Active),
            TestPreparationEntities.ValidMarketRole.Patch(t => t.Function = EicFunction.BillingAgent));

        var doNotReturnActor = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor.Patch(t => t.Status = ActorStatus.Active),
            TestPreparationEntities.ValidMarketRole.Patch(t => t.Function = EicFunction.BillingAgent));

        var user = await _fixture.PrepareUserAsync();
        var userRole = await _fixture.PrepareUserRoleAsync(
            new[] { PermissionId.OrganizationsView },
            EicFunction.BillingAgent);

        await _fixture.AssignUserRoleAsync(user.Id, doNotReturnActor.Id, userRole.Id);

        // Act
        var perms = (await userRepository
            .GetPermissionsAsync(new ActorId(actor.Id), new ExternalUserId(user.ExternalId)))
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
        var userRepository = new UserQueryRepository(context, scope.GetInstance<IPermissionRepository>());

        var user = await _fixture.PrepareUserAsync();
        var actor1 = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor.Patch(t => t.Status = ActorStatus.Active),
            TestPreparationEntities.ValidMarketRole.Patch(t => t.Function = EicFunction.BillingAgent));

        var actor2 = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor.Patch(t => t.Status = ActorStatus.Active),
            TestPreparationEntities.ValidMarketRole.Patch(t => t.Function = EicFunction.GridAccessProvider));

        var userRole1 = await _fixture.PrepareUserRoleAsync(
            new[] { PermissionId.OrganizationsView },
            EicFunction.BillingAgent);

        var userRole2 = await _fixture.PrepareUserRoleAsync(
            new[] { PermissionId.UsersView },
            EicFunction.GridAccessProvider);

        await _fixture.AssignUserRoleAsync(user.Id, actor1.Id, userRole1.Id);
        await _fixture.AssignUserRoleAsync(user.Id, actor2.Id, userRole2.Id);

        // Act
        var permsActor = (await userRepository
            .GetPermissionsAsync(new ActorId(actor1.Id), new ExternalUserId(user.ExternalId)))
            .ToList();

        var permsActor2 = (await userRepository
            .GetPermissionsAsync(new ActorId(actor2.Id), new ExternalUserId(user.ExternalId)))
            .ToList();

        // Assert
        Assert.NotEmpty(permsActor);
        Assert.NotEmpty(permsActor2);
        Assert.Single(permsActor);
        Assert.Single(permsActor2);
        Assert.Equal(PermissionId.OrganizationsView, permsActor.First().Id);
        Assert.Equal(PermissionId.UsersView, permsActor2.First().Id);
    }

    [Fact]
    public async Task GetPermissionsAsync_UserExistWithMultiplePermissionsForActor_ReturnsCorrectPermissions()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var userRepository = new UserQueryRepository(context, scope.GetInstance<IPermissionRepository>());

        var user = await _fixture.PrepareUserAsync();
        var actor = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor.Patch(t => t.Status = ActorStatus.Active),
            TestPreparationEntities.ValidMarketRole.Patch(t => t.Function = EicFunction.DataHubAdministrator),
            TestPreparationEntities.ValidMarketRole.Patch(t => t.Function = EicFunction.GridAccessProvider));

        var userRole1 = await _fixture.PrepareUserRoleAsync(
            new[] { PermissionId.OrganizationsManage, PermissionId.OrganizationsView },
            EicFunction.DataHubAdministrator);

        var userRole2 = await _fixture.PrepareUserRoleAsync(
            new[] { PermissionId.UsersView },
            EicFunction.GridAccessProvider);

        await _fixture.AssignUserRoleAsync(user.Id, actor.Id, userRole1.Id);
        await _fixture.AssignUserRoleAsync(user.Id, actor.Id, userRole2.Id);

        // Act
        var permsActor = (await userRepository
            .GetPermissionsAsync(new ActorId(actor.Id), new ExternalUserId(user.ExternalId)))
            .ToList();

        // Assert
        Assert.NotEmpty(permsActor);
        Assert.Equal(3, permsActor.Count);
        Assert.Contains(PermissionId.OrganizationsManage, permsActor.Select(p => p.Id));
        Assert.Contains(PermissionId.OrganizationsView, permsActor.Select(p => p.Id));
        Assert.Contains(PermissionId.UsersView, permsActor.Select(p => p.Id));
    }

    [Fact]
    public async Task GetPermissionsAsync_UserExistWithPermissionsNotAllowedForEicFunction_ReturnsCorrectPermissions()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var userRepository = new UserQueryRepository(context, scope.GetInstance<IPermissionRepository>());

        var user = await _fixture.PrepareUserAsync();
        var actor = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor.Patch(t => t.Status = ActorStatus.Active),
            TestPreparationEntities.ValidMarketRole.Patch(t => t.Function = EicFunction.BalanceResponsibleParty));

        var userRole = await _fixture.PrepareUserRoleAsync(
            new[] { PermissionId.OrganizationsManage, PermissionId.OrganizationsView },
            EicFunction.BalanceResponsibleParty);

        await _fixture.AssignUserRoleAsync(user.Id, actor.Id, userRole.Id);

        // Act
        var perms = (await userRepository
            .GetPermissionsAsync(new ActorId(actor.Id), new ExternalUserId(user.ExternalId)))
            .ToList();

        // Assert
        Assert.NotEmpty(perms);
        Assert.Equal(PermissionId.OrganizationsView, perms.Single().Id);
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
        var userRepository = new UserQueryRepository(context, scope.GetInstance<IPermissionRepository>());

        var actor = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor.Patch(t =>
            {
                t.IsFas = isFas;
                t.Status = ActorStatus.Active;
            }),
            TestPreparationEntities.ValidMarketRole);

        var user = await _fixture.PrepareUserAsync();
        var userRole = await _fixture.PrepareUserRoleAsync();
        await _fixture.AssignUserRoleAsync(user.Id, actor.Id, userRole.Id);

        // Act
        var actual = await userRepository.IsFasAsync(new ActorId(actor.Id), new ExternalUserId(user.ExternalId));

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
        var userRepository = new UserQueryRepository(context, scope.GetInstance<IPermissionRepository>());

        var actor = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor.Patch(t =>
            {
                t.IsFas = isFas;
                t.Status = ActorStatus.New;
            }),
            TestPreparationEntities.ValidMarketRole);

        var user = await _fixture.PrepareUserAsync();
        var userRole = await _fixture.PrepareUserRoleAsync();
        await _fixture.AssignUserRoleAsync(user.Id, actor.Id, userRole.Id);

        // Act
        var actual = await userRepository
            .IsFasAsync(new ActorId(actor.Id), new ExternalUserId(user.ExternalId));

        // Assert
        Assert.False(actual);
    }
}
