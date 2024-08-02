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
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Revision;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class RevisionLogMiddlewareIntegrationTests : WebApiIntegrationTestsBase<MarketParticipantWebApiAssembly>
{
    private readonly MarketParticipantDatabaseFixture _databaseFixture;
    private readonly Mock<IRevisionActivityPublisher> _revisionActivityPublisherMock = new();

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
        var token = CreateMockedTestToken(testUser.Id, testActor.Id, "user-roles:manage");

        var targetUserRole = await _databaseFixture.PrepareUserRoleAsync();
        var updateUserRoleDto = new UpdateUserRoleDto("new_name", "new_description", UserRoleStatus.Active, []);
        var routeWithRevisionEnabled = $"/user-roles/{targetUserRole.Id}";

        using var httpContent = new StringContent(
            JsonSerializer.Serialize(updateUserRoleDto),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);

        var actual = string.Empty;

        _revisionActivityPublisherMock
            .Setup(pub => pub.PublishAsync(It.IsAny<string>()))
            .Callback(new Action<string>(message => actual = message));

        // Act
        using var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await client.PutAsync(new Uri(routeWithRevisionEnabled, UriKind.Relative), httpContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(string.IsNullOrEmpty(actual));

        var revisionLogEntry = JsonSerializer.Deserialize<RevisionLogEntryDto>(
            actual,
            new JsonSerializerOptions().ConfigureForNodaTime(DateTimeZoneProviders.Tzdb));

        Assert.NotNull(revisionLogEntry);
        Assert.NotEqual(Guid.Empty, revisionLogEntry.LogId);
        Assert.Equal(testUser.Id, revisionLogEntry.UserId);
        Assert.Equal(testActor.Id, revisionLogEntry.ActorId);
        Assert.Equal(RevisionActivities.UserRoleEdited, revisionLogEntry.Activity);
        Assert.Equal(routeWithRevisionEnabled, revisionLogEntry.Origin);
        Assert.Equal(await httpContent.ReadAsStringAsync(), revisionLogEntry.Payload);
        Assert.Equal(nameof(UserRole), revisionLogEntry.AffectedEntityType);
        Assert.Equal(targetUserRole.Id, Guid.Parse(revisionLogEntry.AffectedEntityKey));

        _revisionActivityPublisherMock.Verify(pub => pub.PublishAsync(actual), Times.Once);
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
        var token = CreateMockedTestToken(testUser.Id, testActor.Id, "user-roles:manage");

        var targetUserRole = await _databaseFixture.PrepareUserRoleAsync();
        var updateUserRoleDto = new UpdateUserRoleDto(new string('a', 1024), "new_description", UserRoleStatus.Active, []);
        var routeWithRevisionEnabled = $"/user-roles/{targetUserRole.Id}";

        using var httpContent = new StringContent(
            JsonSerializer.Serialize(updateUserRoleDto),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);

        var actual = string.Empty;

        _revisionActivityPublisherMock
            .Setup(pub => pub.PublishAsync(It.IsAny<string>()))
            .Callback(new Action<string>(message => actual = message));

        // Act
        using var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await client.PutAsync(new Uri(routeWithRevisionEnabled, UriKind.Relative), httpContent);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(string.IsNullOrEmpty(actual));

        var revisionLogEntry = JsonSerializer.Deserialize<RevisionLogEntryDto>(
            actual,
            new JsonSerializerOptions().ConfigureForNodaTime(DateTimeZoneProviders.Tzdb));

        Assert.NotNull(revisionLogEntry);
        Assert.NotEqual(Guid.Empty, revisionLogEntry.LogId);
        Assert.Equal(testUser.Id, revisionLogEntry.UserId);
        Assert.Equal(testActor.Id, revisionLogEntry.ActorId);
        Assert.Equal(RevisionActivities.UserRoleEdited, revisionLogEntry.Activity);
        Assert.Equal(routeWithRevisionEnabled, revisionLogEntry.Origin);
        Assert.Equal(await httpContent.ReadAsStringAsync(), revisionLogEntry.Payload);
        Assert.Equal(nameof(UserRole), revisionLogEntry.AffectedEntityType);
        Assert.Equal(targetUserRole.Id, Guid.Parse(revisionLogEntry.AffectedEntityKey));

        _revisionActivityPublisherMock.Verify(pub => pub.PublishAsync(actual), Times.Once);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            services.Replace(ServiceDescriptor.Scoped(_ => _revisionActivityPublisherMock.Object));
        });
    }
}
