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
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.User;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection("IntegrationTest")]
[IntegrationTest]
public sealed class UserControllerIntegrationTests : WebApiIntegrationTestsBase
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public UserControllerIntegrationTests(MarketParticipantDatabaseFixture fixture)
        : base(fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetAssociatedUserActorsAsync_InvalidToken_Unauthorized()
    {
        // Arrange
        const string target = "user/actors";

        var testToken = new JwtSecurityToken();
        testToken.Payload.AddClaim(new Claim(JwtRegisteredClaimNames.Sub, "E4245D28-599A-44CE-A903-BE6808E6C559"));

        var externalToken = new JwtSecurityTokenHandler().WriteToken(testToken);
        var query = new Uri($"{target}?externalToken={externalToken}", UriKind.Relative);

        using var client = CreateClient();

        // Act
        using var responseJson = await client.GetAsync(query);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, responseJson.StatusCode);
    }

    [Fact]
    public async Task GetAssociatedUserActorsAsync_UnknownUser_ReturnsEmptyList()
    {
        // Arrange
        const string target = "user/actors";

        var testToken = new JwtSecurityToken();
        testToken.Payload.AddClaim(new Claim(JwtRegisteredClaimNames.Sub, "E4245D28-599A-44CE-A903-BE6808E6C559"));

        var externalToken = new JwtSecurityTokenHandler().WriteToken(testToken);
        var query = new Uri($"{target}?externalToken={externalToken}", UriKind.Relative);

        using var client = CreateClient();

        // Act
        Startup.EnableIntegrationTestKeys = true;
        var responseJson = await client.GetStringAsync(query);
        Startup.EnableIntegrationTestKeys = false;

        // Assert
        var response = JsonSerializer.Deserialize<GetActorsAssociatedWithUserResponse>(
            responseJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(response);
        Assert.Empty(response.ActorIds);
    }

    [Fact]
    public async Task GetAssociatedUserActorsAsync_GivenUser_ReturnsEmptyList()
    {
        // Arrange
        const string target = "user/actors";

        var actor = await _fixture.PrepareActorAsync();
        var user = await _fixture.PrepareUserAsync();
        var userRole = await _fixture.PrepareUserRoleAsync(PermissionId.OrganizationsView);
        await _fixture.AssignUserRoleAsync(user.Id, actor.Id, userRole.Id);

        var testToken = new JwtSecurityToken();
        testToken.Payload.AddClaim(new Claim(JwtRegisteredClaimNames.Sub, user.ExternalId.ToString()));

        var externalToken = new JwtSecurityTokenHandler().WriteToken(testToken);
        var query = new Uri($"{target}?externalToken={externalToken}", UriKind.Relative);

        using var client = CreateClient();

        // Act
        Startup.EnableIntegrationTestKeys = true;
        var responseJson = await client.GetStringAsync(query);
        Startup.EnableIntegrationTestKeys = false;

        // Assert
        var response = JsonSerializer.Deserialize<GetActorsAssociatedWithUserResponse>(
            responseJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(response);
        Assert.Equal(actor.Id, response.ActorIds.Single());
    }
}
