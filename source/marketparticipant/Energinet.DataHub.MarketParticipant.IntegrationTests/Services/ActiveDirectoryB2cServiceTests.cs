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
using System.Net;
using System.Threading.Tasks;
using Azure.Identity;
using Energinet.DataHub.Core.FunctionApp.TestCommon.Configuration;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.ActiveDirectory;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Energinet.DataHub.MarketParticipant.Infrastructure;
using Energinet.DataHub.MarketParticipant.Infrastructure.Services;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models.ODataErrors;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Services
{
    [Collection(nameof(IntegrationTestCollectionFixture))]
    [IntegrationTest]
    public sealed class ActiveDirectoryB2CServiceTests
    {
        private readonly IActiveDirectoryB2CService _sut = CreateActiveDirectoryService();

        [Fact]
        public async Task CreateConsumerAppRegistrationAsync_AppIsRegistered_Success()
        {
            ExternalActorId? cleanupId = null;

            try
            {
                // Arrange
                var roles = new List<EicFunction>
                {
                    EicFunction.SystemOperator // transmission system operator
                };

                // Act
                var response = await _sut
                    .CreateAppRegistrationAsync(new MockedGln(), roles)
;

                cleanupId = response.ExternalActorId;

                // Assert
                var app = await _sut.GetExistingAppRegistrationAsync(
                        new AppRegistrationObjectId(Guid.Parse(response.AppObjectId)),
                        new AppRegistrationServicePrincipalObjectId(response.ServicePrincipalObjectId))
;

                Assert.Equal(response.ExternalActorId.Value.ToString(), app.AppId);
            }
            finally
            {
                await CleanupAsync(cleanupId);
            }
        }

        [Fact]
        public async Task GetExistingAppRegistrationAsync_AddTwoRolesToAppRegistration_Success()
        {
            ExternalActorId? cleanupId = null;

            try
            {
                // Arrange
                var roles = new List<EicFunction>
                {
                    EicFunction.SystemOperator, // transmission system operator
                    EicFunction.MeteredDataResponsible
                };

                var createAppRegistrationResponse = await _sut
                    .CreateAppRegistrationAsync(new MockedGln(), roles)
;

                cleanupId = createAppRegistrationResponse.ExternalActorId;

                // Act
                var app = await _sut.GetExistingAppRegistrationAsync(
                        new AppRegistrationObjectId(Guid.Parse(createAppRegistrationResponse.AppObjectId)),
                        new AppRegistrationServicePrincipalObjectId(createAppRegistrationResponse.ServicePrincipalObjectId))
;

                // Assert
                Assert.Equal("d82c211d-cce0-e95e-bd80-c2aedf99f32b", app.AppRoles.First().RoleId);
                Assert.Equal("00e32df2-b846-2e18-328f-702cec8f1260", app.AppRoles.ElementAt(1).RoleId);
            }
            finally
            {
                await CleanupAsync(cleanupId);
            }
        }

        [Fact]
        public async Task DeleteConsumerAppRegistrationAsync_DeleteCreatedAppRegistration_ServiceException404IsThrownWhenTryingToGetTheDeletedApp()
        {
            ExternalActorId? cleanupId = null;

            try
            {
                // Arrange
                var roles = new List<EicFunction>
                {
                    EicFunction.SystemOperator // transmission system operator
                };

                var createAppRegistrationResponse = await _sut.CreateAppRegistrationAsync(
                        new MockedGln(),
                        roles)
;

                cleanupId = createAppRegistrationResponse.ExternalActorId;

                // Act
                await _sut
                    .DeleteAppRegistrationAsync(createAppRegistrationResponse.ExternalActorId)
;

                cleanupId = null;

                // Assert
                var ex = await Assert.ThrowsAsync<ODataError>(async () => await _sut
                        .GetExistingAppRegistrationAsync(
                            new AppRegistrationObjectId(Guid.Parse(createAppRegistrationResponse.AppObjectId)),
                            new AppRegistrationServicePrincipalObjectId(createAppRegistrationResponse.ServicePrincipalObjectId)));

                Assert.Equal((int)HttpStatusCode.NotFound, ex.ResponseStatusCode);
            }
            finally
            {
                await CleanupAsync(cleanupId);
            }
        }

        private static IActiveDirectoryB2CService CreateActiveDirectoryService()
        {
            var integrationTestConfig = new IntegrationTestConfiguration();

            // Graph Service Client
            var clientSecretCredential = new ClientSecretCredential(
                integrationTestConfig.B2CSettings.Tenant,
                integrationTestConfig.B2CSettings.ServicePrincipalId,
                integrationTestConfig.B2CSettings.ServicePrincipalSecret);

            using var graphClient = new GraphServiceClient(
                clientSecretCredential,
                new[]
                {
                    "https://graph.microsoft.com/.default"
                });

            // Azure AD Config
            var config = new AzureAdConfig(
                integrationTestConfig.B2CSettings.BackendServicePrincipalObjectId,
                integrationTestConfig.B2CSettings.BackendAppId);

            // Active Directory Roles
            var activeDirectoryB2CRoles =
                new ActiveDirectoryB2BRolesProvider(graphClient, integrationTestConfig.B2CSettings.BackendAppObjectId);

            // Logger
            var logger = Mock.Of<ILogger<ActiveDirectoryB2CService>>();

            return new ActiveDirectoryB2CService(
                graphClient,
                config,
                activeDirectoryB2CRoles,
                logger);
        }

        private async Task CleanupAsync(ExternalActorId? externalActorId)
        {
            if (externalActorId == null)
                return;

            await _sut
                .DeleteAppRegistrationAsync(externalActorId)
                .ConfigureAwait(false);
        }
    }
}
