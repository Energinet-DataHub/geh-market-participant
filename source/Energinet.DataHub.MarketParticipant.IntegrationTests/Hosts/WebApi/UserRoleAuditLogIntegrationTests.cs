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
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.Core.App.Common.Security;
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoles;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using MediatR;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Hosts.WebApi;

[Collection("IntegrationTest")]
[IntegrationTest]
public sealed class UserRoleAuditLogIntegrationTest : WebApiIntegrationTestsBase
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    public UserRoleAuditLogIntegrationTest(MarketParticipantDatabaseFixture fixture)
        : base(fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Create_UserRole_AuditLogSaved()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var userRoleAuditLogEntryRepository = new UserRoleAuditLogEntryRepository(context);

        var mediator = scope.GetInstance<IMediator>();

        var (_, userId, _) = await _fixture
            .DatabaseManager
            .CreateUserAsync();

        const string name = "Create_UserRole_AuditLogSaved";
        var createUserRoleDto = new CreateUserRoleDto(
            name,
            "description",
            UserRoleStatus.Active,
            EicFunction.Agent,
            new Collection<string>() { Permission.ActorManage.ToString() });

        var createUserRoleCommand = new CreateUserRoleCommand(userId, createUserRoleDto);
        var expectedResult = GenerateCreateLogEntry(createUserRoleDto);

        // Act
        var response = await mediator.Send(createUserRoleCommand);
        var result = await userRoleAuditLogEntryRepository.GetAsync(new UserRoleId(response.UserRoleId));

        // Assert
        var resultList = result.ToList();
        Assert.Single(resultList.Select(e => e.UserRoleId.Value == response.UserRoleId));
        Assert.Single(resultList.Select(e =>
            e.ChangeDescriptionJson.Equals(expectedResult.ChangeDescriptionJson, StringComparison.Ordinal)));
    }

    private static UserRoleAuditLogEntry GenerateCreateLogEntry(CreateUserRoleDto createUserRoleDto)
    {
        var userRoleAuditLogService = new UserRoleAuditLogService();

        var userRole = new UserRole(
            createUserRoleDto.Name,
            createUserRoleDto.Description,
            createUserRoleDto.Status,
            createUserRoleDto.Permissions.Select(Enum.Parse<Permission>),
            createUserRoleDto.EicFunction);

        return userRoleAuditLogService.BuildAuditLogsForUserRoleCreated(
            new UserId(Guid.NewGuid()),
            new UserRoleId(Guid.NewGuid()),
            userRole).First();
    }
}
