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
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoles;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Revision;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Energinet.DataHub.RevisionLog.Integration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class RevisionLogMiddlewareIntegrationTests : WebApiIntegrationTestsBase<MarketParticipantWebApiAssembly>
{
    private readonly MarketParticipantDatabaseFixture _databaseFixture;
    private readonly Mock<IRevisionLogClient> _revisionLogClientMock = new();

    public RevisionLogMiddlewareIntegrationTests(
        MarketParticipantDatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }

    [Fact]
    public async Task UpdateUserRole_WithRevisionEnabled_PublishesLogEntry()
    {
        // Arrange
        var testUser = await _databaseFixture.PrepareUserAsync();
        var testActor = await _databaseFixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor.Patch(a => a.Status = ActorStatus.Active),
            TestPreparationEntities.ValidMarketRole);
        var token = CreateMockedTestToken(testUser.Id, testActor.Id, "user-roles:manage", "users:manage");

        var targetUserRole = await _databaseFixture.PrepareUserRoleAsync();
        var updateUserRoleDto = new UpdateUserRoleDto("new_name", "new_description", UserRoleStatus.Active, []);
        var routeWithRevisionEnabled = $"/user-roles/{targetUserRole.Id}";

        using var httpContent = new StringContent(
            JsonSerializer.Serialize(updateUserRoleDto),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);

        RevisionLogEntry? revisionLogEntry = null;

        _revisionLogClientMock
            .Setup(pub => pub.LogAsync(It.IsAny<RevisionLogEntry>()))
            .Callback(new Action<RevisionLogEntry>(message => revisionLogEntry = message));

        // Act
        using var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await client.PutAsync(new Uri(routeWithRevisionEnabled, UriKind.Relative), httpContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(revisionLogEntry);
        Assert.NotEqual(Guid.Empty, revisionLogEntry.LogId);
        Assert.Equal(testUser.Id, revisionLogEntry.UserId);
        Assert.Equal(testActor.Id, revisionLogEntry.ActorId);
        Assert.Equal("user-roles:manage,users:manage", revisionLogEntry.Permissions);
        Assert.Equal(RevisionActivities.UserRoleEdited, revisionLogEntry.Activity);
        Assert.Equal(routeWithRevisionEnabled, revisionLogEntry.Origin);
        Assert.Equal(await httpContent.ReadAsStringAsync(), revisionLogEntry.Payload);
        Assert.Equal(nameof(UserRole), revisionLogEntry.AffectedEntityType);
        Assert.Equal(targetUserRole.Id, Guid.Parse(revisionLogEntry.AffectedEntityKey!));

        _revisionLogClientMock.Verify(pub => pub.LogAsync(revisionLogEntry), Times.Once);
    }

    [Fact]
    public async Task UpdateUserRole_LargePayload_IsLimited()
    {
        // Arrange
        var testUser = await _databaseFixture.PrepareUserAsync();
        var testActor = await _databaseFixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor.Patch(a => a.Status = ActorStatus.Active),
            TestPreparationEntities.ValidMarketRole);
        var token = CreateMockedTestToken(testUser.Id, testActor.Id, "user-roles:manage", "users:manage");

        var updateUserRoleDto = new UpdateUserRoleDto("new_name", new string('a', 101 * 1024 * 1024), UserRoleStatus.Active, []);
        var routeWithRevisionEnabled = $"/user-roles/{Guid.NewGuid()}";

        using var httpContent = new StringContent(
            JsonSerializer.Serialize(updateUserRoleDto),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);

        RevisionLogEntry? revisionLogEntry = null;

        _revisionLogClientMock
            .Setup(pub => pub.LogAsync(It.IsAny<RevisionLogEntry>()))
            .Callback(new Action<RevisionLogEntry>(message => revisionLogEntry = message));

        // Act
        using var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await client.PutAsync(new Uri(routeWithRevisionEnabled, UriKind.Relative), httpContent);

        // Assert
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.NotNull(revisionLogEntry);
        Assert.Equal(100 * 1024 * 1024, revisionLogEntry.Payload.Length);
    }

    [Fact]
    public async Task UpdateUserRole_BadRequest_StillPublishesLogEntry()
    {
        // Arrange
        var testUser = await _databaseFixture.PrepareUserAsync();
        var testActor = await _databaseFixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor.Patch(a => a.Status = ActorStatus.Active),
            TestPreparationEntities.ValidMarketRole);
        var token = CreateMockedTestToken(testUser.Id, testActor.Id, "user-roles:manage", "users:manage");

        var targetUserRole = await _databaseFixture.PrepareUserRoleAsync();
        var updateUserRoleDto = new UpdateUserRoleDto(new string('a', 1024), "new_description", UserRoleStatus.Active, []);
        var routeWithRevisionEnabled = $"/user-roles/{targetUserRole.Id}";

        using var httpContent = new StringContent(
            JsonSerializer.Serialize(updateUserRoleDto),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);

        RevisionLogEntry? revisionLogEntry = null;

        _revisionLogClientMock
            .Setup(pub => pub.LogAsync(It.IsAny<RevisionLogEntry>()))
            .Callback(new Action<RevisionLogEntry>(message => revisionLogEntry = message));

        // Act
        using var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await client.PutAsync(new Uri(routeWithRevisionEnabled, UriKind.Relative), httpContent);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        Assert.NotNull(revisionLogEntry);
        Assert.NotEqual(Guid.Empty, revisionLogEntry.LogId);
        Assert.Equal(testUser.Id, revisionLogEntry.UserId);
        Assert.Equal(testActor.Id, revisionLogEntry.ActorId);
        Assert.Equal("user-roles:manage,users:manage", revisionLogEntry.Permissions);
        Assert.Equal(RevisionActivities.UserRoleEdited, revisionLogEntry.Activity);
        Assert.Equal(routeWithRevisionEnabled, revisionLogEntry.Origin);
        Assert.Equal(await httpContent.ReadAsStringAsync(), revisionLogEntry.Payload);
        Assert.Equal(nameof(UserRole), revisionLogEntry.AffectedEntityType);
        Assert.Equal(targetUserRole.Id, Guid.Parse(revisionLogEntry.AffectedEntityKey!));

        _revisionLogClientMock.Verify(pub => pub.LogAsync(revisionLogEntry), Times.Once);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            services.Replace(ServiceDescriptor.Scoped(_ => _revisionLogClientMock.Object));
        });
    }
}
