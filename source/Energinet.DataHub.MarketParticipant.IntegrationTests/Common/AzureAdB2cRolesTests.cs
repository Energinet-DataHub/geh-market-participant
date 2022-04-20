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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Infrastructure.Model.ActiveDirectory;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Common
{
    [Collection("IntegrationTest")]
    [IntegrationTest]
    public class AzureAdB2cRolesTests
    {
        private readonly ActiveDirectoryB2CRoles _activeDirectoryB2CRoles;

        public AzureAdB2cRolesTests(ActiveDirectoryB2CRoles activeDirectoryB2CRoles)
        {
            _activeDirectoryB2CRoles = activeDirectoryB2CRoles;
        }

        [Fact]
        public async Task GetActiveDirectoryRoles_AllActiveDirectoryRolesAreDownloaded_Success()
        {
            // Arrange
            await using var host = await IntegrationTestHost.InitializeAsync().ConfigureAwait(false);
            await using var scope = host.BeginScope();

            // Act + Assert
            Assert.Equal(Guid.Parse("882f1309-f696-4055-9b1d-70bd3abd6aec"), _activeDirectoryB2CRoles.DdkId);
            Assert.Equal(Guid.Parse("3ba88b9a-9179-4f03-9281-3e43757b54c7"), _activeDirectoryB2CRoles.DdmId);
            Assert.Equal(Guid.Parse("9ec90757-aac3-40c4-bcb3-8bffcb642996"), _activeDirectoryB2CRoles.DdqId);
            Assert.Equal(Guid.Parse("11b79733-b588-413d-9833-8adedce991aa"), _activeDirectoryB2CRoles.EzId);
            Assert.Equal(Guid.Parse("f312e8a2-5c5d-4bb1-b925-2d9656bcebc2"), _activeDirectoryB2CRoles.MdrId);
        }
    }
}
