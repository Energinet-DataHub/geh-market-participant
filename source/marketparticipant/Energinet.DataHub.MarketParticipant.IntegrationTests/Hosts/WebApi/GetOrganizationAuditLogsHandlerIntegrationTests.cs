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
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.Core.App.Common.Abstractions.Users;
using Energinet.DataHub.MarketParticipant.Application.Commands.Organization;
using Energinet.DataHub.MarketParticipant.Application.Security;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class GetOrganizationAuditLogsHandlerIntegrationTests
{
    private readonly MarketParticipantDatabaseFixture _databaseFixture;

    public GetOrganizationAuditLogsHandlerIntegrationTests(
        MarketParticipantDatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }

    [Fact]
    public Task GetAuditLogs_ChangeName_IsAudited()
    {
        var expected = Guid.NewGuid().ToString();

        return TestAuditOfOrganizationChangeAsync(
            response =>
            {
                var expectedLog = response
                    .OrganizationAuditLogs
                    .Where(log => log.AuditIdentityId != KnownAuditIdentityProvider.TestFramework.IdentityId.Value)
                    .Single(log => log.OrganizationChangeType == OrganizationChangeType.Name);

                Assert.Equal(expected, expectedLog.Value);
            },
            organization =>
            {
                organization.Name = expected;
            });
    }

    [Fact]
    public Task GetAuditLogs_ChangeDomain_IsAudited()
    {
        var expected = new MockedDomain();

        return TestAuditOfOrganizationChangeAsync(
            response =>
            {
                var expectedLog = response
                    .OrganizationAuditLogs
                    .Where(log => log.AuditIdentityId != KnownAuditIdentityProvider.TestFramework.IdentityId.Value)
                    .Single(log => log.OrganizationChangeType == OrganizationChangeType.DomainChange);

                Assert.Equal(expected, expectedLog.Value);
            },
            organization =>
            {
                organization.Domain = expected;
            });
    }

    private async Task TestAuditOfOrganizationChangeAsync(
        Action<GetOrganizationAuditLogsResponse> assert,
        params Action<Domain.Model.Organization>[] changeActions)
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);

        var actorEntity = await _databaseFixture.PrepareActorAsync();
        var auditedUser = await _databaseFixture.PrepareUserAsync();

        var userContext = new Mock<IUserContext<FrontendUser>>();
        userContext
            .Setup(uc => uc.CurrentUser)
            .Returns(new FrontendUser(auditedUser.Id, actorEntity.OrganizationId, actorEntity.Id, false));

        host.ServiceCollection.RemoveAll<IUserContext<FrontendUser>>();
        host.ServiceCollection.AddScoped(_ => userContext.Object);

        await using var scope = host.BeginScope();
        var organizationRepository = scope.ServiceProvider.GetRequiredService<IOrganizationRepository>();

        foreach (var action in changeActions)
        {
            var organization = await organizationRepository.GetAsync(new OrganizationId(actorEntity.OrganizationId));
            Assert.NotNull(organization);

            action(organization);
            await organizationRepository.AddOrUpdateAsync(organization);
        }

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var command = new GetOrganizationAuditLogsCommand(actorEntity.OrganizationId);

        // Act
        var actual = await mediator.Send(command);

        // Assert
        assert(actual);

        var auditLogs = actual
            .OrganizationAuditLogs
            .Where(log => log.AuditIdentityId != KnownAuditIdentityProvider.TestFramework.IdentityId.Value);

        // Skip initial audits.
        foreach (var organizationAuditLog in auditLogs)
        {
            Assert.Equal(auditedUser.Id, organizationAuditLog.AuditIdentityId);
            Assert.Equal(actorEntity.OrganizationId, organizationAuditLog.OrganizationId);
            Assert.True(organizationAuditLog.Timestamp > DateTimeOffset.UtcNow.AddSeconds(-5));
            Assert.True(organizationAuditLog.Timestamp < DateTimeOffset.UtcNow.AddSeconds(5));
        }
    }
}
