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
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
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
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Graph.Models;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection("IntegrationTest")]
[IntegrationTest]
public sealed class TokenControllerIntegrationTests :
    WebApiIntegrationTestsBase,
    IClassFixture<KeyClientFixture>
{
    private readonly KeyClientFixture _keyClientFixture;
    private readonly MarketParticipantDatabaseFixture _marketParticipantDatabaseFixture;
    private readonly GraphServiceClientFixture _graphServiceClientFixture;

    public TokenControllerIntegrationTests(
        KeyClientFixture keyClientFixture,
        MarketParticipantDatabaseFixture marketParticipantDatabaseFixture,
        GraphServiceClientFixture graphServiceClientFixture)
        : base(marketParticipantDatabaseFixture)
    {
        _keyClientFixture = keyClientFixture;
        _marketParticipantDatabaseFixture = marketParticipantDatabaseFixture;
        _graphServiceClientFixture = graphServiceClientFixture;
    }

    [Fact]
    public async Task OpenIdConfiguration_Get_ContainsConfiguration()
    {
        // Arrange
        const string target = ".well-known/openid-configuration";

        using var client = CreateClient();

        // Act
        var rawConfiguration = await client.GetStringAsync(new Uri(target, UriKind.Relative));

        // Assert
        var expectedStructure = new
        {
            issuer = string.Empty,
            jwks_uri = string.Empty
        };

        var configuration = Deserialize(rawConfiguration, expectedStructure);
        Assert.NotNull(configuration);
        Assert.Equal("https://datahub.dk", configuration.issuer);
        Assert.Equal("https://localhost/token/keys", configuration.jwks_uri);
    }

    [Fact]
    public async Task DiscoveryKeys_Get_ContainsKeys()
    {
        // Arrange
        const string target = "token/keys";

        using var client = CreateClient();

        // Act
        var rawConfiguration = await client.GetStringAsync(new Uri(target, UriKind.Relative));

        // Assert
        var expectedStructure = new
        {
            keys = new[]
            {
                new
                {
                    kid = string.Empty,
                    kty = string.Empty,
                    n = string.Empty,
                    e = string.Empty
                }
            }
        };

        var configuration = Deserialize(rawConfiguration, expectedStructure);
        Assert.NotNull(configuration);
        Assert.NotEmpty(configuration.keys);
        Assert.NotEmpty(configuration.keys[0].kid);
        Assert.NotEmpty(configuration.keys[0].kty);
        Assert.NotEmpty(configuration.keys[0].n);
        Assert.NotEmpty(configuration.keys[0].e);
    }

    [Fact]
    public async Task Token_InvalidExternalToken_Returns401()
    {
        // Arrange
        const string target = "token";

        var testUser = await _marketParticipantDatabaseFixture.PrepareUserAsync();
        var externalToken = CreateExternalTestToken(testUser.ExternalId);

        var actorId = Guid.NewGuid();
        var request = new TokenRequest(actorId, externalToken);

        using var httpContent = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);

        using var client = CreateClient();

        // Act
        Startup.EnableIntegrationTestKeys = false;
        using var response = await client.PostAsync(new Uri(target, UriKind.Relative), httpContent);
        Startup.EnableIntegrationTestKeys = true;

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Token_ValidExternalToken_ReturnsInternalToken()
    {
        // Arrange
        const string target = "token";

        var testUser = await _marketParticipantDatabaseFixture.PrepareUserAsync();
        var testActor = await _marketParticipantDatabaseFixture.PrepareActorAsync();
        var externalToken = CreateExternalTestToken(testUser.ExternalId);

        var actorId = testActor.Id;
        var request = new TokenRequest(actorId, externalToken);

        using var httpContent = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);

        using var client = CreateClient();

        // Act
        using var response = await client.PostAsync(new Uri(target, UriKind.Relative), httpContent);
        var responseJson = await response.Content.ReadAsStringAsync();

        // Assert
        var internalTokenJson = JsonSerializer.Deserialize<TokenResponse>(
            responseJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(internalTokenJson);
        Assert.NotNull(internalTokenJson.Token);
    }

    [Fact]
    public async Task Token_ValidExternalTokenButUsersDoesNotExist_Returns401()
    {
        // Arrange
        const string target = "token";

        var externalToken = CreateExternalTestToken(Guid.NewGuid());

        var actorId = Guid.NewGuid();
        var request = new TokenRequest(actorId, externalToken);

        using var httpContent = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);

        using var client = CreateClient();

        // Act
        using var response = await client.PostAsync(new Uri(target, UriKind.Relative), httpContent);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Token_ValidExternalToken_ReturnsValidToken()
    {
        // Arrange
        const string target = "token";

        var testUser = await _marketParticipantDatabaseFixture.PrepareUserAsync();
        var testActor = await _marketParticipantDatabaseFixture.PrepareActorAsync();
        var externalToken = CreateExternalTestToken(testUser.ExternalId);

        var actorId = testActor.Id;
        var request = new TokenRequest(actorId, externalToken);

        using var httpContent = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);

        using var client = CreateClient();

        // Act
        using var response = await client.PostAsync(new Uri(target, UriKind.Relative), httpContent);
        var responseJson = await response.Content.ReadAsStringAsync();

        // Assert
        var internalTokenJson = JsonSerializer.Deserialize<TokenResponse>(
            responseJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(internalTokenJson);

        var validationParameters = new TokenValidationParameters
        {
            ValidAudience = TestBackendAppId,
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ClockSkew = TimeSpan.Zero,
            ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                "http://locahost/.well-known/openid-configuration",
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever(CreateClient()) { RequireHttps = false })
        };

        var result = await new JwtSecurityTokenHandler()
            .ValidateTokenAsync(internalTokenJson.Token, validationParameters);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Token_ValidOpenIdSetup_UserUpdatedWithOpenIdIdentity()
    {
        // Arrange
        const string target = "token";

        var openIdIdentity = new List<ObjectIdentity>()
        {
            new()
            {
                SignInType = "federated",
                Issuer = Guid.NewGuid().ToString(),
                IssuerAssignedId = Guid.NewGuid().ToString()
            }
        };

        var invitedEmailAddress = new MockedEmailAddress();
        var invitedUserExternalId = await _graphServiceClientFixture.CreateUserAsync(invitedEmailAddress);
        var openIdUserExternalUserId = await _graphServiceClientFixture.CreateUserAsync(invitedEmailAddress, openIdIdentity);

        await _marketParticipantDatabaseFixture
            .PrepareUserAsync(TestPreparationEntities
                .UnconnectedUser.Patch(e =>
                {
                    e.ExternalId = invitedUserExternalId.Value;
                    e.Email = invitedEmailAddress;
                }));

        var testActor = await _marketParticipantDatabaseFixture.PrepareActorAsync();
        var externalToken = CreateExternalTestToken(openIdUserExternalUserId.Value);

        var actorId = testActor.Id;
        var request = new TokenRequest(actorId, externalToken);

        using var httpContent = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);

        using var client = CreateClient();

        // Act
        using var response = await client.PostAsync(new Uri(target, UriKind.Relative), httpContent);
        await response.Content.ReadAsStringAsync();

        // Assert
        var updatedUser = await _graphServiceClientFixture.TryFindExternalUserAsync(invitedEmailAddress);

        Assert.NotNull(updatedUser);
        Assert.NotNull(updatedUser.Identities);
        Assert.Equal(3, updatedUser.Identities.Count);
        Assert.Single(updatedUser.Identities, e => e.SignInType == "federated");
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
            new[] { new Claim(JwtRegisteredClaimNames.Sub, externalUserId.ToString()) },
            notBefore ?? DateTime.UtcNow.AddDays(-1),
            expires ?? DateTime.UtcNow.AddDays(1),
            new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(externalToken);
    }

    private static T? Deserialize<T>(string json, T inferType)
    {
        return JsonSerializer.Deserialize<T>(json);
    }
}
