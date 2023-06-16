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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.Infrastructure.Model.Permissions;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection("IntegrationTest")]
[IntegrationTest]
public sealed class ControllerErrorHandlingIntegrationTests : WebApiIntegrationTestsBase
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public ControllerErrorHandlingIntegrationTests(MarketParticipantDatabaseFixture fixture)
        : base(fixture)
    {
        _fixture = fixture;
        AllowAllTokens = true;
    }

    [Fact]
    public async Task GetUser_EmptyId_ReturnsBadRequest()
    {
        // Arrange
        var target = new Uri($"user/{Guid.Empty}", UriKind.Relative);

        using var client = CreateClient();

        var token = await CreateTokenAsync(PermissionId.UsersView);
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

        // Act
        var response = await client.GetAsync(target);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetUser_WrongId_ReturnsNotFound()
    {
        // Arrange
        var target = new Uri($"user/{Guid.NewGuid()}", UriKind.Relative);

        using var client = CreateClient();

        var token = await CreateTokenAsync(PermissionId.UsersView);
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

        // Act
        var response = await client.GetAsync(target);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<string> CreateTokenAsync(PermissionId permission)
    {
        var user = await _fixture.PrepareUserAsync();
        var userRole = await _fixture.PrepareUserRoleAsync(permission);
        var actor = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor.Patch(actor => actor.Status = ActorStatus.Active),
            TestPreparationEntities.ValidMarketRole);

        await _fixture.AssignUserRoleAsync(user.Id, actor.Id, userRole.Id);

        var testToken = new JwtSecurityToken();
        testToken.Payload.AddClaim(new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()));
        testToken.Payload.AddClaim(new Claim(JwtRegisteredClaimNames.Azp, actor.Id.ToString()));
        testToken.Payload.AddClaim(new Claim("role", KnownPermissions.All.Single(p => p.Id == permission).Claim));

        return new JwtSecurityTokenHandler().WriteToken(testToken);
    }
}
