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
using Energinet.DataHub.MarketParticipant.Infrastructure.Extensions;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Microsoft.Graph.Models;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Services
{
    [Collection(nameof(IntegrationTestCollectionFixture))]
    [IntegrationTest]
    public sealed class ActorClientSecretServiceTests
    {
        private readonly IActorClientSecretService _sut;
        private readonly GraphServiceClientFixture _graphServiceClientFixture;
        private readonly B2CFixture _b2CFixture;

        public ActorClientSecretServiceTests(GraphServiceClientFixture graphServiceClientFixture, SecretFixture secretFixture, B2CFixture b2CFixture)
        {
            _graphServiceClientFixture = graphServiceClientFixture;
            _b2CFixture = b2CFixture;
#pragma warning disable CA1062
            _sut = secretFixture.ClientSecretService;
#pragma warning restore CA1062
        }

        [Fact]
        public async Task AddSecretToAppRegistration_ReturnsPassword_AndAppHasPassword()
        {
            // Arrange
            var actor = CreateActor(new[]
            {
                EicFunction.SystemOperator, // transmission system operator
            });

            try
            {
                var createAppRegistrationResponse = await _b2CFixture.B2CService.CreateAppRegistrationAsync(actor);

                actor.ExternalActorId = createAppRegistrationResponse.ExternalActorId;

                // Act
                var result = await _sut
                    .CreateSecretAsync(actor);
                var existing = await GetExistingAppAsync(createAppRegistrationResponse.ExternalActorId);

                // Assert
                Assert.NotEmpty(result.SecretText);
                Assert.NotEmpty(result.SecretId.ToString());
                Assert.NotNull(existing);
                Assert.True(existing.PasswordCredentials is { Count: > 0 });
            }
            finally
            {
                await CleanupAsync(actor);
            }
        }

        [Fact]
        public async Task RemoveSecretFromAppRegistration_DoesNotThrow_And_PasswordIsRemoved()
        {
            var actor = CreateActor(new[]
            {
                EicFunction.SystemOperator, // transmission system operator
            });

            try
            {
                // Arrange
                var createAppRegistrationResponse = await _b2CFixture.B2CService.CreateAppRegistrationAsync(actor);

                actor.ExternalActorId = createAppRegistrationResponse.ExternalActorId;

                await _sut
                    .CreateSecretAsync(actor);

                // Act
                var exceptions = await Record.ExceptionAsync(() => _sut.RemoveSecretAsync(actor));
                var existing = await GetExistingAppAsync(createAppRegistrationResponse.ExternalActorId);

                // Assert
                Assert.Null(exceptions);
                Assert.NotNull(existing);
                Assert.True(existing.PasswordCredentials is { Count: 0 });
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
            await _b2CFixture.B2CService
                .DeleteAppRegistrationAsync(actor)
                .ConfigureAwait(false);
        }

        private async Task<Microsoft.Graph.Models.Application?> GetExistingAppAsync(ExternalActorId externalActorId)
        {
            var appId = externalActorId.Value.ToString();
            var applicationUsingAppId = await _graphServiceClientFixture.Client
                .Applications
                .GetAsync(x => { x.QueryParameters.Filter = $"appId eq '{appId}'"; })
                .ConfigureAwait(false);

            var applications = await applicationUsingAppId!
                .IteratePagesAsync<Microsoft.Graph.Models.Application, ApplicationCollectionResponse>(_graphServiceClientFixture.Client)
                .ConfigureAwait(false);

            return applications.SingleOrDefault();
        }
    }
}
