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
using System.IO;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Services.Email;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Email;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users.Authentication;
using Energinet.DataHub.MarketParticipant.Tests.Common;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Services;

[UnitTest]
public sealed class EmailContentGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_WithTemplate_GeneratesEmailContent()
    {
        // Arrange
        var emailContentGenerator = new EmailContentGenerator();

        var emptyParams = new Dictionary<string, string>();
        var template = new UserInviteEmailTemplate(emptyParams);

        var expected = await GetTestTemplateAsync(EmailTemplateId.UserInvite);

        // Act
        var actual = await emailContentGenerator.GenerateAsync(template, emptyParams);

        // Assert
        Assert.NotNull(actual);
        Assert.NotNull(actual.Subject);
        Assert.Equal(expected, actual.HtmlContent);
    }

    [Fact]
    public async Task GenerateAsync_WithTemplate_GeneratesEmailSubject()
    {
        // Arrange
        var emailContentGenerator = new EmailContentGenerator();

        var emptyParams = new Dictionary<string, string>();
        var template = new UserInviteEmailTemplate(emptyParams);

        // Act
        var actual = await emailContentGenerator.GenerateAsync(template, emptyParams);

        // Assert
        Assert.NotNull(actual);
        Assert.NotNull(actual.HtmlContent);
        Assert.Equal("Invitation til DataHub {environment_short}", actual.Subject);
    }

    [Fact]
    public async Task GenerateAsync_WithParameters_ReplacesParamsInContent()
    {
        const string firstName = "John";
        const string organizationName = "Test Organization";
        const string actorGln = "5790002221149";
        const string actorName = "Test Actor";

        // Arrange
        var emailContentGenerator = new EmailContentGenerator();

        var userIdentity = new UserIdentity(
            new ExternalUserId(Guid.Empty),
            new MockedEmailAddress(),
            UserIdentityStatus.Active,
            firstName,
            "fake_value",
            null,
            DateTimeOffset.UtcNow,
            AuthenticationMethod.Undetermined,
            Array.Empty<LoginIdentity>());

        var organization = new Organization(
            new OrganizationId(Guid.Empty),
            organizationName,
            MockedBusinessRegisterIdentifier.New(),
            new Address(null, null, null, null, "DK"),
            new MockedDomain(),
            OrganizationStatus.Active);

        var actor = new Actor(
            new ActorId(Guid.Empty),
            organization.Id,
            null,
            new GlnActorNumber(actorGln),
            ActorStatus.Active,
            Array.Empty<ActorMarketRole>(),
            new ActorName(actorName),
            null);

        var emptyParams = new Dictionary<string, string>();
        var template = new UserInviteEmailTemplate(userIdentity, organization, actor);

        // Act
        var actual = await emailContentGenerator.GenerateAsync(template, emptyParams);

        // Assert
        Assert.NotNull(actual);
        Assert.NotNull(actual.HtmlContent);
        Assert.Contains($"<h1>Hej {firstName}</h1>", actual.HtmlContent, StringComparison.Ordinal);
        Assert.Contains($"<h2>{organizationName} - {actorGln} {actorName}</h2>", actual.HtmlContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GenerateAsync_WithAdditionalParameters_ReplacesParamsInContent()
    {
        const string inviteLink = "https://datahub.dk";

        // Arrange
        var emailContentGenerator = new EmailContentGenerator();

        var additionalParams = new Dictionary<string, string>
        {
            { "invite_link", inviteLink }
        };

        var emptyParams = new Dictionary<string, string>();
        var template = new UserInviteEmailTemplate(emptyParams);

        // Act
        var actual = await emailContentGenerator.GenerateAsync(template, additionalParams);

        // Assert
        Assert.NotNull(actual);
        Assert.NotNull(actual.HtmlContent);
        Assert.Contains($"href=\"{inviteLink}\"", actual.HtmlContent, StringComparison.Ordinal);
    }

    private static async Task<string> GetTestTemplateAsync(EmailTemplateId emailTemplateId)
    {
        var assembly = typeof(EmailContentGenerator).Assembly;
        var resourceName = $"Energinet.DataHub.MarketParticipant.Application.Services.Email.Templates.{emailTemplateId}.html";

        var templateStream = assembly.GetManifestResourceStream(resourceName);
        Assert.NotNull(templateStream);

        await using (templateStream.ConfigureAwait(false))
        {
            using (var reader = new StreamReader(templateStream))
            {
                return await reader.ReadToEndAsync().ConfigureAwait(false);
            }
        }
    }
}
