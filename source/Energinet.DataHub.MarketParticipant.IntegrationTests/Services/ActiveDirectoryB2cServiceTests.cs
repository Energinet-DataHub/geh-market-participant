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
using System.Net;
using System.Threading.Tasks;
using Azure.Identity;
using Energinet.DataHub.Core.FunctionApp.TestCommon.Configuration;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.BusinessRoles;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Energinet.DataHub.MarketParticipant.Infrastructure;
using Energinet.DataHub.MarketParticipant.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Services
{
    [Collection("IntegrationTest")]
    [IntegrationTest]
    public sealed class ActiveDirectoryB2cServiceTests : IAsyncDisposable
    {
        private readonly IActiveDirectoryService _sut = null!;
        private readonly List<string> _b2cAppRegistrationIds = null!;

        public ActiveDirectoryB2cServiceTests()
        {
            _sut = CreateActiveDirectoryService();
            _b2cAppRegistrationIds = new List<string>();
        }

        public async ValueTask DisposeAsync()
        {
            foreach (var appRegistrationId in _b2cAppRegistrationIds)
            {
                await _sut.DeleteAppRegistrationAsync(appRegistrationId).ConfigureAwait(false);
            }
        }

        [Fact]
        public async Task CreateConsumerAppRegistrationAsync_AppIsRegistered_Success()
        {
            // Arrange
            var roles = new List<MarketRole>
            {
                new(EicFunction.SystemOperator), // transmission system operator
            };

            // Act
            var response = await _sut.CreateAppRegistrationAsync(
                "TemporaryTestApp",
                roles)
                .ConfigureAwait(false);

            // Assert
            var app = await _sut.GetExistingAppRegistrationAsync(
                response.AppObjectId,
                response.ServicePrincipalObjectId)
                .ConfigureAwait(false);

            _b2cAppRegistrationIds.Add(app.AppObjectId);

            Assert.Equal(response.ExternalActorId.Value.ToString(), app.AppId);
        }

        ////[Fact]
        ////public async Task CreateConsumerAppRegistrationAsync_AppIsRegistered_Success()
        ////{
        ////    // Arrange
        ////    var roles = new List<MarketRole>
        ////    {
        ////        new(EicFunction.SystemOperator), // transmission system operator
        ////    };

        ////    // Act
        ////    var response = await _activeDirectoryService.CreateAppRegistrationAsync(
        ////        "TemporaryTestApp",
        ////        roles)
        ////        .ConfigureAwait(false);

        ////    var app = await _activeDirectoryService.GetExistingAppRegistrationAsync(
        ////        response.AppObjectId,
        ////        response.ServicePrincipalObjectId)
        ////        .ConfigureAwait(false);

        ////    _b2cAppRegistrationIds.Add(app.AppObjectId);

        ////    // Assert
        ////    Assert.Equal(response.ExternalActorId.Value.ToString(), app.AppId);
        ////}

        ////[Fact]
        ////public async Task GetExistingAppRegistrationAsync_AddTwoRolesToAppRegistration_Success()
        ////{
        ////    // Arrange
        ////    var roles = new List<MarketRole>
        ////    {
        ////        new(EicFunction.SystemOperator), // transmission system operator
        ////        new(EicFunction.EnergySupplier)
        ////    };

        ////    var createAppRegistrationResponse = await _activeDirectoryService.CreateAppRegistrationAsync(
        ////            "TemporaryTestAppWithTwoRoles",
        ////            roles)
        ////        .ConfigureAwait(false);

        ////    // Act
        ////    var app = await _activeDirectoryService.GetExistingAppRegistrationAsync(
        ////            createAppRegistrationResponse.AppObjectId,
        ////            createAppRegistrationResponse.ServicePrincipalObjectId)
        ////        .ConfigureAwait(false);

        ////    _b2cAppRegistrationIds.Add(app.AppObjectId);

        ////    // Assert
        ////    Assert.Equal("11b79733-b588-413d-9833-8adedce991aa", app.AppRoles.Roles[0].RoleId);
        ////    Assert.Equal("9ec90757-aac3-40c4-bcb3-8bffcb642996", app.AppRoles.Roles[1].RoleId);
        ////}

        ////[Fact]
        ////public async Task DeleteConsumerAppRegistrationAsync_DeleteCreatedAppRegistration_ServiceException404IsThrownWhenTryingToGetTheDeletedApp()
        ////{
        ////    // Arrange
        ////    var roles = new List<MarketRole>
        ////    {
        ////        new(EicFunction.SystemOperator), // transmission system operator
        ////    };

        ////    var createAppRegistrationResponse = await _activeDirectoryService.CreateAppRegistrationAsync(
        ////            "TemporaryTestAppWithTwoRoles",
        ////            roles)
        ////        .ConfigureAwait(false);

        ////    // Act
        ////    await _activeDirectoryService
        ////        .DeleteAppRegistrationAsync(createAppRegistrationResponse.AppObjectId)
        ////        .ConfigureAwait(false);

        ////    // Assert
        ////    var ex = await Assert.ThrowsAsync<ServiceException>(async () => await _activeDirectoryService
        ////            .GetExistingAppRegistrationAsync(
        ////            createAppRegistrationResponse.AppObjectId,
        ////            createAppRegistrationResponse.ServicePrincipalObjectId)
        ////        .ConfigureAwait(false))
        ////        .ConfigureAwait(false);

        ////    Assert.Equal(HttpStatusCode.NotFound, ex.StatusCode);
        ////}

        private static IActiveDirectoryService CreateActiveDirectoryService()
        {
            // Graph Client
            var integrationTestConfig = new IntegrationTestConfiguration();

            var clientSecretCredential = new ClientSecretCredential(
                integrationTestConfig.B2CSettings.Tenant,
                integrationTestConfig.B2CSettings.ServicePrincipalId,
                integrationTestConfig.B2CSettings.ServicePrincipalSecret);

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
                new BalancePowerSupplierRole(),
                new GridAccessProviderRole(),
                new BalanceResponsiblePartyRole()
            });

            // Logger
            var logger = Mock.Of<ILogger<ActiveDirectoryB2cService>>();

            return new ActiveDirectoryB2cService(
                graphClient,
                config,
                businessRoleCodeDomainService,
                logger);
        }
    }
}
