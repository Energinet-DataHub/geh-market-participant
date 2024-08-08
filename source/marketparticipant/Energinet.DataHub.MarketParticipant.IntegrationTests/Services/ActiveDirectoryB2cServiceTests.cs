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
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Energinet.DataHub.MarketParticipant.Infrastructure.Services;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Services;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class ActiveDirectoryB2CServiceTests
{
    private readonly IActiveDirectoryB2CService _sut;
    private readonly IActiveDirectoryB2BRolesProvider _rolesProvider;
    private readonly GraphServiceClientFixture _graphServiceClientFixture;

    public ActiveDirectoryB2CServiceTests(GraphServiceClientFixture graphServiceClientFixture, B2CFixture b2CFixture)
    {
        ArgumentNullException.ThrowIfNull(b2CFixture);

        _graphServiceClientFixture = graphServiceClientFixture;
        _sut = b2CFixture.B2CService;
        _rolesProvider = b2CFixture.B2CRolesProvider;
    }

    [Fact]
    public async Task CreateConsumerAppRegistrationAsync_AppIsRegistered_Success()
    {
        // Arrange
        var actor = CreateActor(new[]
        {
            EicFunction.SystemOperator
        });

        try
        {
            // Act
            await _sut.AssignApplicationRegistrationAsync(actor);

            // Assert
            var app = await _graphServiceClientFixture.GetExistingAppRegistrationAsync(actor.ExternalActorId!.ToString());

            Assert.Equal(actor.ExternalActorId.Value.ToString(), app.AppId);
        }
        finally
        {
            await CleanupAsync(actor);
        }
    }

    [Fact]
    public async Task GetExistingAppRegistrationAsync_AddTwoRolesToAppRegistration_Success()
    {
        // Arrange
        var actor = CreateActor(new[]
        {
            EicFunction.SystemOperator, // transmission system operator
            EicFunction.MeteredDataResponsible
        });
        try
        {
            await _sut.AssignApplicationRegistrationAsync(actor);

            var appRoles = await _rolesProvider.GetB2BRolesAsync();
            var systemOperatorRole = appRoles.EicRolesMapped[EicFunction.SystemOperator];
            var meteredDataResponsibleRole = appRoles.EicRolesMapped[EicFunction.MeteredDataResponsible];

            // Act
            var app = await _graphServiceClientFixture.GetExistingAppRegistrationAsync(actor.ExternalActorId!.ToString());

            // Assert
            var actual = app.AppRoles.ToList();
            Assert.Equal(2, actual.Count);
            Assert.Contains(systemOperatorRole.ToString(), actual.Select(x => x.RoleId));
            Assert.Contains(meteredDataResponsibleRole.ToString(), actual.Select(x => x.RoleId));
        }
        finally
        {
            await CleanupAsync(actor);
        }
    }

    [Fact]
    public async Task DeleteConsumerAppRegistrationAsync_DeleteCreatedAppRegistration_ServiceException404IsThrownWhenTryingToGetTheDeletedApp()
    {
        // Arrange
        var actor = CreateActor(new[]
        {
            EicFunction.SystemOperator, // transmission system operator
        });

        try
        {
            await _sut.AssignApplicationRegistrationAsync(actor);

            var externalActorId = actor.ExternalActorId!.Value.ToString();

            // Act
            await _sut.DeleteAppRegistrationAsync(actor);

            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await _graphServiceClientFixture
                .GetExistingAppRegistrationAsync(externalActorId));
        }
        finally
        {
            await CleanupAsync(actor);
        }
    }

    private static Actor CreateActor(IEnumerable<EicFunction> roles)
    {
        return new Actor(
            new ActorId(Guid.NewGuid()),
            new OrganizationId(Guid.NewGuid()),
            null,
            new MockedGln(),
            ActorStatus.Active,
            roles.Select(x => new ActorMarketRole(x)),
            new ActorName(Guid.NewGuid().ToString()),
            null);
    }

    private async Task CleanupAsync(Actor actor)
    {
        await _sut
            .DeleteAppRegistrationAsync(actor)
            .ConfigureAwait(false);
    }
}
