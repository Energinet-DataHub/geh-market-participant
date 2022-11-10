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

using System.Threading.Tasks;
using Azure.Identity;
using Energinet.DataHub.Core.FunctionApp.TestCommon.Configuration;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.BusinessRoles;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Energinet.DataHub.MarketParticipant.Domain.Services.ActiveDirectory;
using Energinet.DataHub.MarketParticipant.Infrastructure.Services;
using Energinet.DataHub.MarketParticipant.Infrastructure.Services.ActiveDirectory;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Services.ActiveDirectory
{
    [Collection("IntegrationTest")]
    [IntegrationTest]
    public sealed class ActiveDirectoryServiceTests
    {
        private readonly IActiveDirectoryService _sut = CreateActiveDirectoryService();

        [Fact]
        public async Task CreateActor_Success()
        {
            // Arrange
            var actorGln = new MockedGln();
            var actor = new Actor(ActorNumber.Create(actorGln));
            try
            {
              // Act
              var result = await _sut
                    .CreateOrUpdateAppAsync(actor)
                    .ConfigureAwait(false);

                // Assert
              Assert.False(string.IsNullOrEmpty(result));
            }
            finally
            {
                // Cleanup
                await _sut
                    .DeleteActorAsync(actor)
                    .ConfigureAwait(false);
            }
        }

        [Fact]
        public async Task CreateActor_IsIdemPotent_Success()
        {
            // Arrange
            var actorGln = new MockedGln();
            var actor = new Actor(ActorNumber.Create(actorGln));
            try
            {
                // Act
                var result = await _sut
                    .CreateOrUpdateAppAsync(actor)
                    .ConfigureAwait(false);

                var result2 = await _sut
                    .CreateOrUpdateAppAsync(actor)
                    .ConfigureAwait(false);

                // Assert
                Assert.False(string.IsNullOrEmpty(result));
                Assert.False(string.IsNullOrEmpty(result2));
                Assert.True(result == result2);
            }
            finally
            {
                // Cleanup
                await _sut
                    .DeleteActorAsync(actor)
                    .ConfigureAwait(false);
            }
        }

        [Fact]
        public async Task DeleteActor_Success()
        {
            // Arrange
            var actor = new Actor(ActorNumber.Create(new MockedGln()));

            // Create actor to delete
            var actorToDelete = await _sut
                .CreateOrUpdateAppAsync(actor)
                .ConfigureAwait(false);

            // Act
            await _sut
                .DeleteActorAsync(actor)
                .ConfigureAwait(false);

            var exists = await _sut.AppExistsAsync(actor);

            // Assert
            Assert.False(exists);
        }

        private static IActiveDirectoryService CreateActiveDirectoryService()
        {
            var integrationTestConfig = new IntegrationTestConfiguration();
            // Graph Service Client
            var clientSecretCredential = new ClientSecretCredential(
                integrationTestConfig.B2CSettings.Tenant,
                integrationTestConfig.B2CSettings.ServicePrincipalId,
                integrationTestConfig.B2CSettings.ServicePrincipalSecret);

            var graphClient = new GraphServiceClient(
                clientSecretCredential,
                new[] { "https://graph.microsoft.com/.default" });

            // Logger
            var logger = Mock.Of<ILogger<ActiveDirectoryService>>();

            return new ActiveDirectoryService(
                graphClient,
                logger);
        }
    }
}
