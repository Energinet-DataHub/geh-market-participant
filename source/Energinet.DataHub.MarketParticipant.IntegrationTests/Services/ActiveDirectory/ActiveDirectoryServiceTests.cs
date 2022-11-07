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
using Energinet.DataHub.MarketParticipant.Domain.Model.BusinessRoles;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Energinet.DataHub.MarketParticipant.Domain.Services.ActiveDirectory;
using Energinet.DataHub.MarketParticipant.Infrastructure;
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
        public async Task ListActors_ReturnMoreThanZero()
        {
            // Arrange
            // Act
            var response = await _sut
                .ListAppsAsync()
                .ConfigureAwait(false);

            // Assert
            Assert.True(response.Any());
        }

        [Fact]
        public async Task CreateActor_Success()
        {
            var actorId = string.Empty;
            try
            {
                // Arrange
                // Act
              actorId = await _sut
                    .CreateAppAsync(
                        new BusinessRegisterIdentifier(
                            Guid.NewGuid().ToString()),
                        "JJ Test Actor")
                    .ConfigureAwait(false);

                // Assert
              Assert.False(string.IsNullOrEmpty(actorId));
            }
            finally
            {
                // Cleanup
                if (!string.IsNullOrEmpty(actorId))
                {
                    await _sut
                        .DeleteAppAsync(actorId)
                        .ConfigureAwait(false);
                }
            }
        }

        [Fact]
        public async Task DeleteActor_Success()
        {
            // Arrange
            // Create actor to delete
            var actorIdToDelete = await _sut
                .CreateAppAsync(new BusinessRegisterIdentifier(Guid.NewGuid().ToString()), "JJ Test Actor")
                .ConfigureAwait(false);

            // Act
            await _sut
                .DeleteAppAsync(actorIdToDelete)
                .ConfigureAwait(false);

            var exists = await _sut.AppExistsAsync(actorIdToDelete);

            // Assert
            Assert.False(exists);
        }

        private static IActiveDirectoryService CreateActiveDirectoryService()
        {
            var integrationTestConfig = new IntegrationTestConfiguration();

            // Graph Service Client
            var clientSecretCredential = new ClientSecretCredential(
                "dhtitans.onmicrosoft.com",
                "2b914bca-26d7-4d9b-a22e-8305f3259097",
                "rqH8Q~R~Thgcf1fHsHd.wZ4IMnMmqS9p7rqK7ckU");

            var graphClient = new GraphServiceClient(
                clientSecretCredential,
                new[] { "https://graph.microsoft.com/.default" });

            // Azure AD Config
            var config = new AzureAdConfig(
                integrationTestConfig.B2CSettings.BackendServicePrincipalObjectId,
                integrationTestConfig.B2CSettings.BackendAppId);

            // Business Role Code Domain Service
            var businessRoleCodeDomainService = new BusinessRoleCodeDomainService(new IBusinessRole[]
            {
                new MeteredDataResponsibleRole(), new SystemOperatorRole()
            });

            // Active Directory Roles
            var activeDirectoryB2CRoles =
                new ActiveDirectoryB2CRolesProvider(graphClient, integrationTestConfig.B2CSettings.BackendAppObjectId);

            // Logger
            var logger = Mock.Of<ILogger<ActiveDirectoryService>>();

            return new ActiveDirectoryService(
                graphClient,
                config,
                businessRoleCodeDomainService,
                activeDirectoryB2CRoles,
                logger);
        }
    }
}
