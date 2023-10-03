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
using Energinet.DataHub.MarketParticipant.Application.Commands.Permissions;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class UpdatePermissionHandlerIntegrationTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public UpdatePermissionHandlerIntegrationTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task UpdatePermission_UpdateDescription()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);

        var frontendUser = await _fixture.PrepareUserAsync();
        host.ServiceCollection.MockFrontendUser(frontendUser.Id);

        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var userRoleWithPermission = await _fixture.PrepareUserRoleAsync();
        var newPermissionDescription = Guid.NewGuid().ToString();

        var updateCommand = new UpdatePermissionCommand(
            (int)userRoleWithPermission.Permissions[0].Permission,
            newPermissionDescription);

        var getPermissionDetails = new GetPermissionDetailsCommand(userRoleWithPermission.EicFunctions[0].EicFunction);

        // Act
        await mediator.Send(updateCommand);
        var response = await mediator.Send(getPermissionDetails);

        // Assert
        Assert.Single(response.Permissions, p => p.Description == newPermissionDescription);
    }

    [Fact]
    public async Task UpdatePermission_UpdateDescription_AuditLogsCreated()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var permissionAuditLogEntryRepository = new PermissionAuditLogEntryRepository(context);

        var frontendFirstUser = await _fixture.PrepareUserAsync();
        host.ServiceCollection.MockFrontendUser(frontendFirstUser.Id);

        var userRoleWithPermission = await _fixture.PrepareUserRoleAsync(PermissionId.OrganizationsManage);
        var targetPermission = (int)userRoleWithPermission.Permissions[0].Permission;

        await using var scopeFirstUser = host.BeginScope();
        var mediatorFirstUser = scopeFirstUser.ServiceProvider.GetRequiredService<IMediator>();

        var frontendSecondUser = await _fixture.PrepareUserAsync();
        host.ServiceCollection.MockFrontendUser(frontendSecondUser);

        await using var scopeSecondUser = host.BeginScope();
        var mediatorSecondUser = scopeSecondUser.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        await mediatorFirstUser.Send(new UpdatePermissionCommand(targetPermission, Guid.NewGuid().ToString()));
        await mediatorSecondUser.Send(new UpdatePermissionCommand(targetPermission, Guid.NewGuid().ToString()));

        // Assert
        var logs = await permissionAuditLogEntryRepository
            .GetAsync(userRoleWithPermission.Permissions[0].Permission)
            .ConfigureAwait(false);

        Assert.Single(logs, p =>
            p.Permission == userRoleWithPermission.Permissions[0].Permission &&
            p.PermissionChangeType == PermissionChangeType.DescriptionChange &&
            p.AuditIdentity.Value == frontendFirstUser.Id);

        Assert.Single(logs, p =>
            p.Permission == userRoleWithPermission.Permissions[0].Permission &&
            p.PermissionChangeType == PermissionChangeType.DescriptionChange &&
            p.AuditIdentity.Value == frontendSecondUser.Id);
    }
}
