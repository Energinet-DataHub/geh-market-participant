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
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.Core.App.Common.Abstractions.Users;
using Energinet.DataHub.MarketParticipant.Application.Commands.Actor;
using Energinet.DataHub.MarketParticipant.Application.Security;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using NodaTime.Extensions;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class GetActorAuditLogsHandlerIntegrationTests
{
    private readonly Guid _externalId = Guid.NewGuid();

    private readonly MarketParticipantDatabaseFixture _databaseFixture;

    public GetActorAuditLogsHandlerIntegrationTests(
        MarketParticipantDatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }

    [Fact]
    public async Task GetAuditLogs_Created_ReturnsSingleAudit()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);

        var actorEntity = await _databaseFixture.PrepareActorAsync();

        await using var scope = host.BeginScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var command = new GetActorAuditLogsCommand(actorEntity.Id);

        // Act
        var actual = await mediator.Send(command);

        // Assert
        Assert.Empty(actual.ActorContactAuditLogs);

        var actorCreatedAudit = actual.ActorAuditLogs.Single();
        Assert.Equal(actorEntity.Id, actorCreatedAudit.ActorId);
        Assert.Equal(ActorChangeType.Created, actorCreatedAudit.ActorChangeType);
        Assert.True(actorCreatedAudit.Timestamp > DateTimeOffset.UtcNow.AddSeconds(-5));
        Assert.True(actorCreatedAudit.Timestamp < DateTimeOffset.UtcNow.AddSeconds(5));
    }

    [Fact]
    public Task GetAuditLogs_ChangeName_IsAudited()
    {
        var expected = new ActorName(Guid.NewGuid().ToString());

        return TestAuditOfActorChangeAsync(
            response =>
            {
                var expectedLog = response.ActorAuditLogs.Single(log => log.ActorChangeType == ActorChangeType.Name);

                Assert.Equal(expected.Value, expectedLog.CurrentValue);
                Assert.Equal(TestPreparationEntities.ValidActor.Name, expectedLog.PreviousValue);
            },
            actor =>
            {
                actor.Name = expected;
            });
    }

    [Fact]
    public Task GetAuditLogs_ChangeStatus_IsAudited()
    {
        return TestAuditOfActorChangeAsync(
            response =>
            {
                var expectedLog = response.ActorAuditLogs.Single(log => log.ActorChangeType == ActorChangeType.Status);

                Assert.Equal(ActorStatus.Active.ToString(), expectedLog.CurrentValue);
                Assert.Equal(ActorStatus.New.ToString(), expectedLog.PreviousValue);
            },
            actor =>
            {
                actor.Status = ActorStatus.Active;
            });
    }

    [Fact]
    public Task GetAuditLogs_ChangeSecretCredentials_IsAudited()
    {
        var expected = new ActorClientSecretCredentials(
            _externalId,
            Guid.NewGuid(),
            DateTimeOffset.Parse("2020-01-01", CultureInfo.InvariantCulture).ToInstant());

        return TestAuditOfActorChangeAsync(
            response =>
            {
                var expectedLog = response.ActorAuditLogs.Single(log => log.ActorChangeType == ActorChangeType.SecretCredentials);

                Assert.Equal(expected.ExpirationDate.ToString("g", CultureInfo.InvariantCulture), expectedLog.CurrentValue);
                Assert.Equal(string.Empty, expectedLog.PreviousValue);
            },
            actor =>
            {
                actor.Credentials = expected;
            });
    }

    [Fact]
    public Task GetAuditLogs_ChangeCertificateCredentials_IsAudited()
    {
        var expected = new ActorCertificateCredentials(
            Guid.NewGuid().ToString(),
            "mocked",
            DateTimeOffset.Parse("2021-01-01", CultureInfo.InvariantCulture).ToInstant());

        return TestAuditOfActorChangeAsync(
            response =>
            {
                var expectedLog = response.ActorAuditLogs.Single(log => log.ActorChangeType == ActorChangeType.CertificateCredentials);

                Assert.Equal(expected.CertificateThumbprint, expectedLog.CurrentValue);
                Assert.Equal(string.Empty, expectedLog.PreviousValue);
            },
            actor =>
            {
                actor.Credentials = expected;
            });
    }

    [Fact]
    public Task GetAuditLogs_RemoveCertificateCredentials_IsAudited()
    {
        var expected = new ActorCertificateCredentials(
            Guid.NewGuid().ToString(),
            "mocked_kv_identifier",
            DateTimeOffset.Parse("2021-01-01", CultureInfo.InvariantCulture).ToInstant());

        return TestAuditOfActorChangeAsync(
            response =>
            {
                var expectedLogs = response
                    .ActorAuditLogs
                    .Where(log => log.ActorChangeType == ActorChangeType.CertificateCredentials)
                    .ToList();

                Assert.Equal(2, expectedLogs.Count);
                Assert.Equal(expected.CertificateThumbprint, expectedLogs[0].CurrentValue);
                Assert.Equal(string.Empty, expectedLogs[0].PreviousValue);
                Assert.Equal(string.Empty, expectedLogs[1].CurrentValue);
                Assert.Equal(expected.CertificateThumbprint, expectedLogs[1].PreviousValue);
            },
            actor =>
            {
                actor.Credentials = expected;
            },
            actor =>
            {
                actor.Credentials = null;
            });
    }

    [Fact]
    public Task GetAuditLogs_RemoveClientSecretCredentials_IsAudited()
    {
        var expected = new ActorClientSecretCredentials(
            _externalId,
            Guid.NewGuid(),
            DateTimeOffset.Parse("2021-01-01", CultureInfo.InvariantCulture).ToInstant());

        return TestAuditOfActorChangeAsync(
            response =>
            {
                var expectedLogs = response
                    .ActorAuditLogs
                    .Where(log => log.ActorChangeType == ActorChangeType.SecretCredentials)
                    .ToList();

                Assert.Equal(2, expectedLogs.Count);
                Assert.Equal(expected.ExpirationDate.ToString("g", CultureInfo.InvariantCulture), expectedLogs[0].CurrentValue);
                Assert.Equal(string.Empty, expectedLogs[0].PreviousValue);
                Assert.Equal(string.Empty, expectedLogs[1].CurrentValue);
                Assert.Equal(expected.ExpirationDate.ToString("g", CultureInfo.InvariantCulture), expectedLogs[1].PreviousValue);
            },
            actor =>
            {
                actor.Credentials = expected;
            },
            actor =>
            {
                actor.Credentials = null;
            });
    }

    [Fact]
    public Task GetAuditLogs_ChangeActorContactName_IsAudited()
    {
        var initialName = Guid.NewGuid().ToString();
        var changedName = Guid.NewGuid().ToString();
        var emailAddress = new MockedEmailAddress();

        return TestAuditOfActorContactChangeAsync(
            response =>
            {
                var expectedLog = response.ActorContactAuditLogs.Single(log => log.ActorContactChangeType == ActorContactChangeType.Name);

                Assert.Equal(changedName, expectedLog.CurrentValue);
                Assert.Equal(initialName, expectedLog.PreviousValue);
            },
            actorId => new ActorContact(actorId, initialName, ContactCategory.Default, emailAddress, null),
            actorId => new ActorContact(actorId, changedName, ContactCategory.Default, emailAddress, null));
    }

    [Fact]
    public Task GetAuditLogs_ChangeActorContactEmail_IsAudited()
    {
        var initialEmail = new MockedEmailAddress();
        var changedEmail = new MockedEmailAddress();

        return TestAuditOfActorContactChangeAsync(
            response =>
            {
                var expectedLog = response.ActorContactAuditLogs.Single(log => log.ActorContactChangeType == ActorContactChangeType.Email);

                Assert.Equal(changedEmail, expectedLog.CurrentValue);
                Assert.Equal(initialEmail, expectedLog.PreviousValue);
            },
            actorId => new ActorContact(actorId, "mocked", ContactCategory.Default, initialEmail, null),
            actorId => new ActorContact(actorId, "mocked", ContactCategory.Default, changedEmail, null));
    }

    [Fact]
    public Task GetAuditLogs_ChangeActorContactPhone_IsAudited()
    {
        var initialPhone = new PhoneNumber("+45 12345678");
        var changedPhone = new PhoneNumber("+45 87654321");
        var emailAddress = new MockedEmailAddress();

        return TestAuditOfActorContactChangeAsync(
            response =>
            {
                var expectedLog = response.ActorContactAuditLogs.Single(log => log.ActorContactChangeType == ActorContactChangeType.Phone);

                Assert.Equal(changedPhone.Number, expectedLog.CurrentValue);
                Assert.Equal(initialPhone.Number, expectedLog.PreviousValue);
            },
            actorId => new ActorContact(actorId, "mocked", ContactCategory.Default, emailAddress, initialPhone),
            actorId => new ActorContact(actorId, "mocked", ContactCategory.Default, emailAddress, changedPhone));
    }

    [Fact]
    public Task GetAuditLogs_DeleteActorContact_IsAudited()
    {
        return TestAuditOfActorContactChangeAsync(
            response =>
            {
                var expectedCreated = response.ActorContactAuditLogs.Single(log => log.ActorContactChangeType == ActorContactChangeType.Created);
                var expectedDeleted = response.ActorContactAuditLogs.Single(log => log.ActorContactChangeType == ActorContactChangeType.Deleted);

                Assert.NotNull(expectedCreated);
                Assert.NotNull(expectedDeleted);
            },
            actorId => new ActorContact(actorId, "mocked", ContactCategory.Default, new MockedEmailAddress(), null),
            _ => null);
    }

    private async Task TestAuditOfActorChangeAsync(
        Action<GetActorAuditLogsResponse> assert,
        params Action<Actor>[] changeActions)
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);

        var actorEntity = await _databaseFixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor.Patch(a => a.ActorId = _externalId),
            TestPreparationEntities.ValidMarketRole);

        var userContext = new Mock<IUserContext<FrontendUser>>();

        host.ServiceCollection.RemoveAll<IUserContext<FrontendUser>>();
        host.ServiceCollection.AddScoped(_ => userContext.Object);

        await using var scope = host.BeginScope();

        var actorRepository = scope.ServiceProvider.GetRequiredService<IActorRepository>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new GetActorAuditLogsCommand(actorEntity.Id);
        var auditLogsProcessed = 1; // Skip 1, as first log is always Created.

        foreach (var action in changeActions)
        {
            var auditedUser = await _databaseFixture.PrepareUserAsync();

            userContext
                .Setup(uc => uc.CurrentUser)
                .Returns(new FrontendUser(auditedUser.Id, actorEntity.OrganizationId, actorEntity.Id, false));

            var actor = await actorRepository.GetAsync(new ActorId(actorEntity.Id));
            Assert.NotNull(actor);

            action(actor);
            await actorRepository.AddOrUpdateAsync(actor);

            var auditLogs = await mediator.Send(command);

            foreach (var actorAuditLog in auditLogs.ActorAuditLogs.Skip(auditLogsProcessed))
            {
                Assert.Equal(auditedUser.Id, actorAuditLog.AuditIdentityId);
                Assert.Equal(actorEntity.Id, actorAuditLog.ActorId);
                Assert.True(actorAuditLog.Timestamp > DateTimeOffset.UtcNow.AddSeconds(-5));
                Assert.True(actorAuditLog.Timestamp < DateTimeOffset.UtcNow.AddSeconds(5));

                auditLogsProcessed++;
            }
        }

        // Act
        var actual = await mediator.Send(command);

        // Assert
        assert(actual);
    }

    private async Task TestAuditOfActorContactChangeAsync(
        Action<GetActorAuditLogsResponse> assert,
        params Func<ActorId, ActorContact?>[] contactGenerator)
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);

        var actorEntity = await _databaseFixture.PrepareActorAsync();

        var userContext = new Mock<IUserContext<FrontendUser>>();

        host.ServiceCollection.RemoveAll<IUserContext<FrontendUser>>();
        host.ServiceCollection.AddScoped(_ => userContext.Object);

        await using var scope = host.BeginScope();

        var actorContactRepository = scope.ServiceProvider.GetRequiredService<IActorContactRepository>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var actorId = new ActorId(actorEntity.Id);
        var command = new GetActorAuditLogsCommand(actorEntity.Id);
        var auditLogsProcessed = 0;

        foreach (var generator in contactGenerator)
        {
            var auditedUser = await _databaseFixture.PrepareUserAsync();

            userContext
                .Setup(uc => uc.CurrentUser)
                .Returns(new FrontendUser(auditedUser.Id, actorEntity.OrganizationId, actorEntity.Id, false));

            var contact = generator(actorId);

            var existing = await actorContactRepository.GetAsync(actorId);
            var toRemove = existing.Where(c => contact == null || c.Category == contact.Category);

            foreach (var existingContact in toRemove)
            {
                await actorContactRepository.RemoveAsync(existingContact);
            }

            if (contact != null)
            {
                await actorContactRepository.AddAsync(contact);
            }

            var auditLogs = await mediator.Send(command);

            foreach (var actorAuditLog in auditLogs.ActorContactAuditLogs.Skip(auditLogsProcessed))
            {
                Assert.Equal(auditedUser.Id, actorAuditLog.AuditIdentityId);
                Assert.Equal(actorEntity.Id, actorAuditLog.ActorId);
                Assert.True(actorAuditLog.Timestamp > DateTimeOffset.UtcNow.AddSeconds(-5));
                Assert.True(actorAuditLog.Timestamp < DateTimeOffset.UtcNow.AddSeconds(5));

                auditLogsProcessed++;
            }
        }

        // Act
        var actual = await mediator.Send(command);

        // Assert
        assert(actual);
    }
}
