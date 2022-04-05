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

using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Microsoft.Graph;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Services
{
    [Collection("IntegrationTest")]
    [IntegrationTest]
    public sealed class ActiveDirectoryB2cServiceTests : IAsyncLifetime
    {
        private IActiveDirectoryService _activeDirectoryService = null!;
        private List<string> _b2cAppRegistrationIds = null!;

        public async Task InitializeAsync()
        {
            await using var host = await IntegrationTestHost.InitializeAsync().ConfigureAwait(false);
            var scope = host.BeginScope();
            _activeDirectoryService = scope.GetInstance<IActiveDirectoryService>();

            _b2cAppRegistrationIds = new List<string>();
        }

        async Task IAsyncLifetime.DisposeAsync()
        {
            foreach (var appRegistrationId in _b2cAppRegistrationIds)
            {
                await _activeDirectoryService.DeleteAppRegistrationAsync(appRegistrationId).ConfigureAwait(false);
            }
        }

        [Fact]
        public async Task GetAllRoles()
        {
            var app = await _activeDirectoryService.GetExistingAppRegistrationAsync(
                    "b1ce2b68-518f-4989-aae7-2d612992d8ab",
                    "178010e1-8784-4656-b98c-066b8fcca278")
                .ConfigureAwait(false);

            _b2cAppRegistrationIds.Add(app.AppObjectId);

            // Assert
            Assert.Equal("roles[0]", app.AppRoles.Roles[0].RoleId);
        }

        [Fact]
        public async Task CreateConsumerAppRegistrationAsync_AppIsRegistered_Success()
        {
            // Arrange
            var roles = new[]
            {
                "11b79733-b588-413d-9833-8adedce991aa", // transmission system operator
            };

            // Act
            var response = await _activeDirectoryService.CreateAppRegistrationAsync(
                "TemporaryTestApp",
                roles)
                .ConfigureAwait(false);

            var app = await _activeDirectoryService.GetExistingAppRegistrationAsync(
                response.AppObjectId,
                response.ServicePrincipalObjectId)
                .ConfigureAwait(false);

            _b2cAppRegistrationIds.Add(app.AppObjectId);

            // Assert
            Assert.Equal(response.ExternalActorId.Value.ToString(), app.AppId);
        }

        [Fact]
        public async Task GetExistingAppRegistrationAsync_AddTwoRolesToAppRegistration_Success()
        {
            // Arrange
            var roles = new[]
            {
                "11b79733-b588-413d-9833-8adedce991aa", // transmission system operator
                "9ec90757-aac3-40c4-bcb3-8bffcb642996" // Electrical supplier
            };

            var createAppRegistrationResponse = await _activeDirectoryService.CreateAppRegistrationAsync(
                    "TemporaryTestAppWithTwoRoles",
                    roles)
                .ConfigureAwait(false);

            // Act
            var app = await _activeDirectoryService.GetExistingAppRegistrationAsync(
                    createAppRegistrationResponse.AppObjectId,
                    createAppRegistrationResponse.ServicePrincipalObjectId)
                .ConfigureAwait(false);

            _b2cAppRegistrationIds.Add(app.AppObjectId);

            // Assert
            Assert.Equal(roles[0], app.AppRoles.Roles[0].RoleId);
            Assert.Equal(roles[1], app.AppRoles.Roles[1].RoleId);
        }

        [Fact]
        public async Task DeleteConsumerAppRegistrationAsync_DeleteCreatedAppRegistration_ServiceException404IsThrownWhenTryingToGetTheDeletedApp()
        {
            // Arrange
            var roles = new[]
            {
                "11b79733-b588-413d-9833-8adedce991aa", // transmission system operator
            };

            var createAppRegistrationResponse = await _activeDirectoryService.CreateAppRegistrationAsync(
                    "TemporaryTestAppWithTwoRoles",
                    roles)
                .ConfigureAwait(false);

            // Act
            await _activeDirectoryService
                .DeleteAppRegistrationAsync(createAppRegistrationResponse.AppObjectId)
                .ConfigureAwait(false);

            // Assert
            var ex = await Assert.ThrowsAsync<ServiceException>(async () => await _activeDirectoryService
                    .GetExistingAppRegistrationAsync(
                    createAppRegistrationResponse.AppObjectId,
                    createAppRegistrationResponse.ServicePrincipalObjectId)
                .ConfigureAwait(false))
                .ConfigureAwait(false);

            Assert.Equal(HttpStatusCode.NotFound, ex.StatusCode);
        }
    }
}
