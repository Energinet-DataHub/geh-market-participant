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
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Hosting;
using Microsoft.IdentityModel.Tokens;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection("IntegrationTest")]
[IntegrationTest]
public sealed class TokenPartsControllerIntegrationTests :
    WebApiIntegrationTestsBase,
    IClassFixture<KeyClientFixture>
{
    private readonly KeyClientFixture _keyClientFixture;

    public TokenPartsControllerIntegrationTests(
        KeyClientFixture keyClientFixture,
        MarketParticipantDatabaseFixture fixture)
        : base(fixture)
    {
        _keyClientFixture = keyClientFixture;
    }

    [Fact]
    public async Task Token_Issuer_IsKnown()
    {
        // Arrange
        var externalToken = CreateExternalTestToken();

        // Act
        var internalToken = await FetchTokenAsync(externalToken);

        // Assert
        Assert.Equal("https://datahub.dk", internalToken.Issuer);
    }

    [Fact]
    public async Task Token_Audience_IsKnown()
    {
        // Arrange
        var externalToken = CreateExternalTestToken();

        // Act
        var internalToken = await FetchTokenAsync(externalToken);

        // Assert
        Assert.Equal(TestBackendAppId, internalToken.Audiences.Single());
    }

    [Fact]
    public async Task Token_UserId_IsKnown()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var externalToken = CreateExternalTestToken(userId: userId);

        // Act
        var internalToken = await FetchTokenAsync(externalToken);

        // Assert
        Assert.Equal(userId, internalToken.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
    }

    [Fact]
    public async Task Token_ActorId_IsKnown()
    {
        // Arrange
        var actorId = Guid.NewGuid();
        var externalToken = CreateExternalTestToken();

        // Act
        var internalToken = await FetchTokenAsync(externalToken, actorId);

        // Assert
        Assert.Equal(actorId.ToString(), internalToken.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Azp).Value);
    }

    [Fact]
    public async Task Token_Role_IsKnown()
    {
        // Arrange
        var externalToken = CreateExternalTestToken();

        // Act
        var internalToken = await FetchTokenAsync(externalToken);

        // Assert
        Assert.NotEmpty(internalToken.Claims.Where(c => c.Type == "role"));
    }

    [Fact]
    public async Task Token_NotBefore_IsValid()
    {
        // Arrange
        var notBefore = DateTime.UtcNow.Date.AddDays(Random.Shared.Next(3));
        var externalToken = CreateExternalTestToken(
            notBefore: notBefore,
            expires: notBefore.AddDays(1));

        // Act
        var internalToken = await FetchTokenAsync(externalToken);

        // Assert
        Assert.Equal(notBefore, internalToken.ValidFrom);
    }

    [Fact]
    public async Task Token_Expires_IsValid()
    {
        // Arrange
        var expires = DateTime.UtcNow.Date.AddDays(Random.Shared.Next(3));
        var externalToken = CreateExternalTestToken(expires: expires);

        // Act
        var internalToken = await FetchTokenAsync(externalToken);

        // Assert
        Assert.Equal(expires, internalToken.ValidTo);
    }

    [Fact]
    public async Task Token_Type_IsValid()
    {
        // Arrange
        var externalToken = CreateExternalTestToken();

        // Act
        var internalToken = await FetchTokenAsync(externalToken);

        // Assert
        Assert.Equal(JwtConstants.TokenType, internalToken.Header[JwtHeaderParameterNames.Typ]);
    }

    [Fact]
    public async Task Token_Algorithm_IsValid()
    {
        // Arrange
        var externalToken = CreateExternalTestToken();

        // Act
        var internalToken = await FetchTokenAsync(externalToken);

        // Assert
        Assert.Equal(SecurityAlgorithms.RsaSha256, internalToken.Header[JwtHeaderParameterNames.Alg]);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        Startup.EnableIntegrationTestKeys = true;

        builder.UseSetting(Settings.KeyVault.Key, _keyClientFixture.KeyClient.VaultUri.ToString());
        builder.UseSetting(Settings.KeyName.Key, _keyClientFixture.KeyName);
        builder.UseSetting(Settings.RolesValidationEnabled.Key, "true");
    }

    private static string CreateExternalTestToken(
        string userId = "8539CCC6-F098-426D-8ED6-13A2442B0F76",
        DateTime? notBefore = null,
        DateTime? expires = null)
    {
        var key = RandomNumberGenerator.GetBytes(256);

        var externalToken = new JwtSecurityToken(
            "https://example.com",
            "audience",
            new[] { new Claim(JwtRegisteredClaimNames.Sub, userId) },
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
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(internalTokenJson);

        return new JwtSecurityToken(internalTokenJson.Token);
    }
}
