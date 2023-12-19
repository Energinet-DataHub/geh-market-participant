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
using Energinet.DataHub.Core.App.Common.Abstractions.Users;
using Energinet.DataHub.MarketParticipant.Application.Commands.Permissions;
using Energinet.DataHub.MarketParticipant.Application.Security;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Model.Permissions;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class GetPermissionAuditLogsHandlerIntegrationTests
{
    private readonly MarketParticipantDatabaseFixture _databaseFixture;

    public GetPermissionAuditLogsHandlerIntegrationTests(
        MarketParticipantDatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }

    [Fact]
    public Task GetAuditLogs_CreatedClaim_IsAudited()
    {
        var expected = KnownPermissions
            .All
            .Single(kp => kp.Id == PermissionId.ActorCredentialsManage);

        return TestAuditOfPermissionChangeAsync(
            response =>
            {
                var expectedLog = response
                    .AuditLogs
                    .Single(log => log.Change == PermissionAuditedChange.Claim);

                Assert.Equal(expected.Claim, expectedLog.CurrentValue);
                Assert.Equal(expected.Created.ToDateTimeOffset(), expectedLog.Timestamp);
            });
    }

    [Fact]
    public Task GetAuditLogs_ChangeDescription_IsAudited()
    {
        var expected = Guid.NewGuid().ToString();

        return TestAuditOfPermissionChangeAsync(
            response =>
            {
                var expectedLog = response
                    .AuditLogs
                    .Single(log => log.Change == PermissionAuditedChange.Description);

                Assert.Equal(expected, expectedLog.CurrentValue);
            },
            organization =>
            {
                organization.Description = expected;
            });
    }

    private async Task TestAuditOfPermissionChangeAsync(
        Action<PermissionAuditLogsResponse> assert,
        params Action<Permission>[] changeActions)
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);

        var actorEntity = await _databaseFixture.PrepareActorAsync();
        var auditedUser = await _databaseFixture.PrepareUserAsync();
        var permissionId = PermissionId.ActorCredentialsManage;

        var userContext = new Mock<IUserContext<FrontendUser>>();
        userContext
            .Setup(uc => uc.CurrentUser)
            .Returns(new FrontendUser(auditedUser.Id, actorEntity.OrganizationId, actorEntity.Id, false));

        host.ServiceCollection.RemoveAll<IUserContext<FrontendUser>>();
        host.ServiceCollection.AddScoped(_ => userContext.Object);

        await using var scope = host.BeginScope();
        var permissionRepository = scope.ServiceProvider.GetRequiredService<IPermissionRepository>();

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var command = new GetPermissionAuditLogsCommand((int)permissionId);
        var existing = await mediator.Send(command);

        foreach (var action in changeActions)
        {
            var organization = await permissionRepository.GetAsync(permissionId);
            Assert.NotNull(organization);

            action(organization);
            await permissionRepository.UpdatePermissionAsync(organization);
        }

        // Act
        var actual = await mediator.Send(command);

        // Assert
        assert(actual);

        foreach (var permissionAuditLog in actual.AuditLogs.Skip(existing.AuditLogs.Count()))
        {
            Assert.Equal(auditedUser.Id, permissionAuditLog.AuditIdentityId);
            Assert.True(permissionAuditLog.Timestamp > DateTimeOffset.UtcNow.AddSeconds(-5));
            Assert.True(permissionAuditLog.Timestamp < DateTimeOffset.UtcNow.AddSeconds(5));
        }
    }
}
