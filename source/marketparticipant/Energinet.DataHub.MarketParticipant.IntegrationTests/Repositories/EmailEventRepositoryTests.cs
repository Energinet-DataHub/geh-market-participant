﻿// Copyright 2020 Energinet DataHub A/S
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
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model.Email;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class EmailEventRepositoryTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public EmailEventRepositoryTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public static TheoryData<EmailTemplate> EmailTemplates { get; } = new(
        typeof(EmailTemplate)
            .Assembly
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(EmailTemplate)))
            .Select(t => (EmailTemplate)Activator.CreateInstance(t, new Dictionary<string, string>())!));

    [Theory]
    [MemberData(nameof(EmailTemplates))]
    public async Task Insert_EmailTemplate_CanBeReadBack(EmailTemplate emailTemplate)
    {
        // Arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        await using var context2 = _fixture.DatabaseManager.CreateDbContext();

        var emailEventRepository = new EmailEventRepository(context);

        var emailRandom = new RandomlyGeneratedEmailAddress();
        var newEmailEvent = new EmailEvent(emailRandom, emailTemplate);

        // act
        await emailEventRepository.InsertAsync(newEmailEvent);

        // assert
        var emailEventRepository2 = new EmailEventRepository(context2);
        var savedEvents = await emailEventRepository2.GetAllPendingEmailEventsAsync();
        Assert.Single(savedEvents, e =>
            e.Email.Equals(emailRandom) &&
            e.EmailTemplate.GetType() == emailTemplate.GetType());
    }

    [Fact]
    public async Task MarkAsSent()
    {
        // arrange
        await using var host = await OrganizationIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context1 = _fixture.DatabaseManager.CreateDbContext();
        await using var context2 = _fixture.DatabaseManager.CreateDbContext();
        await using var context3 = _fixture.DatabaseManager.CreateDbContext();

        var emailEventRepository1 = new EmailEventRepository(context1);

        var emailRandom = new RandomlyGeneratedEmailAddress();
        var newEmailEvent = new EmailEvent(emailRandom, new UserInviteEmailTemplate(new Dictionary<string, string>()));

        // act
        await emailEventRepository1.InsertAsync(newEmailEvent);

        var emailEventRepository2 = new EmailEventRepository(context2);
        var savedEvents = (await emailEventRepository2
                .GetAllPendingEmailEventsAsync())
            .ToList();
        var elementToMarkAsSent = savedEvents.Single(e => e.Email.Equals(emailRandom));
        await emailEventRepository2.MarkAsSentAsync(elementToMarkAsSent);

        // assert
        var emailEventRepository3 = new EmailEventRepository(context3);
        var savedEventsSent = (await emailEventRepository3
                .GetAllPendingEmailEventsAsync())
            .ToList();

        Assert.All(savedEventsSent, e => Assert.NotEqual(emailRandom, e.Email));
    }
}
