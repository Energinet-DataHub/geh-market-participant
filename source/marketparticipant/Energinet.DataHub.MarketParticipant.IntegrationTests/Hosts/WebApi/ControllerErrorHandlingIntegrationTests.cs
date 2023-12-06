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
using System.Net.Http;
using System.Net.Mime;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Organization;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Extensions;
using Energinet.DataHub.MarketParticipant.Infrastructure.Model.Permissions;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection(nameof(IntegrationTestCollectionFixture))]
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

        var errors = await ReadErrorResponseAsync(response);
        Assert.Single(errors);

        var badArgumentError = errors.Single();
        Assert.NotEmpty(badArgumentError.Message);
        Assert.NotNull(badArgumentError.Args["param"]);
        Assert.NotNull(badArgumentError.Args["value"]);
        Assert.Equal("market_participant.bad_argument.missing_required_value", badArgumentError.Code);
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

        var errors = await ReadErrorResponseAsync(response);
        Assert.Single(errors);

        var badArgumentError = errors.Single();
        Assert.NotEmpty(badArgumentError.Message);
        Assert.NotNull(badArgumentError.Args["id"]);
        Assert.Equal("market_participant.bad_argument.not_found", badArgumentError.Code);
    }

    [Fact]
    public async Task CreateOrganization_WrongLength_ReturnsBadRequest()
    {
        // Arrange
        var target = new Uri("organization", UriKind.Relative);

        using var client = CreateClient();

        var token = await CreateTokenAsync(PermissionId.OrganizationsManage);
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

        var request = new CreateOrganizationDto(
            new string('a', 1000),
            MockedBusinessRegisterIdentifier.New().Identifier,
            new AddressDto(null, null, null, null, "DK"),
            new MockedDomain());

        using var httpContent = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);

        // Act
        var response = await client.PostAsync(target, httpContent);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var errors = await ReadErrorResponseAsync(response);
        Assert.Single(errors);

        var badArgumentError = errors.Single();
        Assert.NotEmpty(badArgumentError.Message);
        Assert.NotNull(badArgumentError.Args["param"]);
        Assert.NotNull(badArgumentError.Args["value"]);
        Assert.NotNull(badArgumentError.Args["min"]);
        Assert.NotNull(badArgumentError.Args["max"]);
        Assert.Equal("market_participant.bad_argument.invalid_length", badArgumentError.Code);
    }

    [Fact]
    public async Task CreateOrganization_NoValue_ReturnsBadRequest()
    {
        // Arrange
        var target = new Uri("organization", UriKind.Relative);

        using var client = CreateClient();

        var token = await CreateTokenAsync(PermissionId.OrganizationsManage);
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

        var request = new CreateOrganizationDto(
            string.Empty,
            MockedBusinessRegisterIdentifier.New().Identifier,
            new AddressDto(null, null, null, null, "DK"),
            new MockedDomain());

        using var httpContent = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);

        // Act
        var response = await client.PostAsync(target, httpContent);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var errors = await ReadErrorResponseAsync(response);

        var badArgumentError = errors.Single(err => err.Code == "market_participant.bad_argument.missing_required_value");
        Assert.NotEmpty(badArgumentError.Message);
        Assert.NotNull(badArgumentError.Args["param"]);
    }

    [Fact]
    public async Task CreateOrganization_DomainError_ReturnsBadRequest()
    {
        // Arrange
        var target = new Uri("organization", UriKind.Relative);

        using var client = CreateClient();

        var token = await CreateTokenAsync(PermissionId.OrganizationsManage);
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

        var request = new CreateOrganizationDto(
            "Mocked Organization",
            MockedBusinessRegisterIdentifier.New().Identifier,
            new AddressDto(null, null, null, null, "DK"),
            new MockedDomain());

        using var httpContent = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);

        var firstResponse = await client.PostAsync(target, httpContent);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        // Act
        var response = await client.PostAsync(target, httpContent);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var errors = await ReadErrorResponseAsync(response);

        var badArgumentError = errors.Single(err => err.Code == "market_participant.validation.organization.business_register_identifier.reserved");
        Assert.NotEmpty(badArgumentError.Message);
        Assert.NotNull(badArgumentError.Args["identifier"]);
    }

    private static async Task<ErrorDescriptor[]> ReadErrorResponseAsync(HttpResponseMessage response)
    {
        var responseContent = await response.Content.ReadAsStringAsync();

        var returnValue = new { errors = Array.Empty<ErrorDescriptor>() };
        var errorResponse = JsonSerializer.Deserialize(responseContent, returnValue.GetType(), new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(errorResponse);
        returnValue = (dynamic)errorResponse;
        return returnValue.errors;
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
        testToken.Payload.AddClaim(new Claim("multitenancy", "true"));

        return new JwtSecurityTokenHandler().WriteToken(testToken);
    }
}
