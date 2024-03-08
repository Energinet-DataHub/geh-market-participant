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
using System.Net.Http;
using System.Net.Mime;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Authorization;
using Energinet.DataHub.MarketParticipant.Common.Configuration;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi;
using Energinet.DataHub.MarketParticipant.Infrastructure.Model.Permissions;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Hosting;
using Microsoft.IdentityModel.Tokens;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class TokenPartsControllerIntegrationTests :
    WebApiIntegrationTestsBase<Startup>,
    IClassFixture<KeyClientFixture>
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly KeyClientFixture _keyClientFixture;
    private readonly MarketParticipantDatabaseFixture _fixture;

    public TokenPartsControllerIntegrationTests(
        KeyClientFixture keyClientFixture,
        MarketParticipantDatabaseFixture fixture)
        : base(fixture)
    {
        _keyClientFixture = keyClientFixture;
        _fixture = fixture;
    }

    [Fact]
    public async Task Token_Issuer_IsKnown()
    {
        // Arrange
        var testUser = await _fixture.PrepareUserAsync();
        var testActor = await _fixture.PrepareActorAsync();
        var externalToken = CreateExternalTestToken(testUser.ExternalId);

        // Act
        var internalToken = await FetchTokenAsync(externalToken, testActor.Id);

        // Assert
        Assert.Equal("https://datahub.dk", internalToken.Issuer);
    }

    [Fact]
    public async Task Token_Audience_IsKnown()
    {
        // Arrange
        var testUser = await _fixture.PrepareUserAsync();
        var testActor = await _fixture.PrepareActorAsync();
        var externalToken = CreateExternalTestToken(testUser.ExternalId);

        // Act
        var internalToken = await FetchTokenAsync(externalToken, testActor.Id);

        // Assert
        Assert.Equal(TestBackendAppId, internalToken.Audiences.Single());
    }

    [Fact]
    public async Task Token_UserId_IsPresent()
    {
        // Arrange
        var testUser = await _fixture.PrepareUserAsync();
        var testActor = await _fixture.PrepareActorAsync();
        var externalToken = CreateExternalTestToken(testUser.ExternalId);

        // Act
        var internalToken = await FetchTokenAsync(externalToken, testActor.Id);

        // Assert
        Assert.Single(internalToken.Claims, c => c.Type == JwtRegisteredClaimNames.Sub && Guid.Parse(c.Value) == testUser.Id);
    }

    [Fact]
    public async Task Token_ActorId_IsKnown()
    {
        // Arrange
        var testUser = await _fixture.PrepareUserAsync();
        var testActor = await _fixture.PrepareActorAsync();
        var externalToken = CreateExternalTestToken(testUser.ExternalId);

        // Act
        var internalToken = await FetchTokenAsync(externalToken, testActor.Id);

        // Assert
        Assert.Equal(testActor.Id.ToString(), internalToken.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Azp).Value);
        Assert.Empty(internalToken.Claims.Where(c => c.Type == "role"));
    }

    [Fact]
    public async Task Token_Role_IsKnown()
    {
        // Arrange
        var usersView = PermissionId.UsersView;

        var actor = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor.Patch(t => t.Status = ActorStatus.Active),
            TestPreparationEntities.ValidMarketRole);

        var user = await _fixture.PrepareUserAsync();
        var userRole = await _fixture.PrepareUserRoleAsync(usersView);
        await _fixture.AssignUserRoleAsync(user.Id, actor.Id, userRole.Id);

        var externalToken = CreateExternalTestToken(user.ExternalId);

        // Act
        var internalToken = await FetchTokenAsync(externalToken, actor.Id);

        // Assert
        var organizationViewClaim = KnownPermissions.All.Single(kp => kp.Id == usersView).Claim;
        Assert.NotEmpty(internalToken.Claims.Where(c => c.Type == "role" && c.Value == organizationViewClaim));
    }

    [Fact]
    public async Task Token_NotBefore_IsValid()
    {
        // Arrange
        var notBefore = DateTime.UtcNow.Date.AddDays(RandomNumberGenerator.GetInt32(3));
        var testUser = await _fixture.PrepareUserAsync();
        var testActor = await _fixture.PrepareActorAsync();
        var externalToken = CreateExternalTestToken(
            testUser.ExternalId,
            notBefore,
            notBefore.AddDays(1));

        // Act
        var internalToken = await FetchTokenAsync(externalToken, testActor.Id);

        // Assert
        Assert.Equal(notBefore, internalToken.ValidFrom);
    }

    [Fact]
    public async Task Token_Expires_IsValid()
    {
        // Arrange
        var expires = DateTime.UtcNow.Date.AddDays(RandomNumberGenerator.GetInt32(3));
        var testUser = await _fixture.PrepareUserAsync();
        var testActor = await _fixture.PrepareActorAsync();
        var externalToken = CreateExternalTestToken(testUser.ExternalId, expires: expires);

        // Act
        var internalToken = await FetchTokenAsync(externalToken, testActor.Id);

        // Assert
        Assert.Equal(expires, internalToken.ValidTo);
    }

    [Fact]
    public async Task Token_Type_IsValid()
    {
        // Arrange
        var testUser = await _fixture.PrepareUserAsync();
        var testActor = await _fixture.PrepareActorAsync();
        var externalToken = CreateExternalTestToken(testUser.ExternalId);

        // Act
        var internalToken = await FetchTokenAsync(externalToken, testActor.Id);

        // Assert
        Assert.Equal(JwtConstants.TokenType, internalToken.Header[JwtHeaderParameterNames.Typ]);
    }

    [Fact]
    public async Task Token_Algorithm_IsValid()
    {
        // Arrange
        var testUser = await _fixture.PrepareUserAsync();
        var testActor = await _fixture.PrepareActorAsync();
        var externalToken = CreateExternalTestToken(testUser.ExternalId);

        // Act
        var internalToken = await FetchTokenAsync(externalToken, testActor.Id);

        // Assert
        Assert.Equal(SecurityAlgorithms.RsaSha256, internalToken.Header[JwtHeaderParameterNames.Alg]);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        base.ConfigureWebHost(builder);
        Startup.EnableIntegrationTestKeys = true;

        builder.UseSetting(Settings.TokenKeyVault.Key, _keyClientFixture.KeyClient.VaultUri.ToString());
        builder.UseSetting(Settings.TokenKeyName.Key, _keyClientFixture.KeyName);
    }

    private static string CreateExternalTestToken(
        Guid externalUserId,
        DateTime? notBefore = null,
        DateTime? expires = null)
    {
        var key = RandomNumberGenerator.GetBytes(256);

        var externalToken = new JwtSecurityToken(
            "https://example.com",
            "audience",
            new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, externalUserId.ToString()),
            },
            notBefore ?? DateTime.UtcNow.AddDays(-1),
            expires ?? DateTime.UtcNow.AddDays(1),
            new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(externalToken);
    }

    private async Task<JwtSecurityToken> FetchTokenAsync(string externalToken, Guid? actorId = null)
    {
        const string target = "token";

        var request = new TokenRequest(actorId ?? Guid.NewGuid(), externalToken);

        using var httpContent = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);

        using var client = CreateClient();

        using var response = await client.PostAsync(new Uri(target, UriKind.Relative), httpContent);
        var responseJson = await response.Content.ReadAsStringAsync();

        var internalTokenJson = JsonSerializer.Deserialize<TokenResponse>(
            responseJson,
            _jsonSerializerOptions);

        Assert.NotNull(internalTokenJson);

        return new JwtSecurityToken(internalTokenJson.Token);
    }
}
