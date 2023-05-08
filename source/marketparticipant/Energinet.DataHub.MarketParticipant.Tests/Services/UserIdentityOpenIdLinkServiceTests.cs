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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users.Authentication;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Tests.Common;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Services
{
    [UnitTest]
    public sealed class UserIdentityOpenIdLinkServiceTests
    {
        [Fact]
        public async Task ValidateAndSetupOpenIdAsync_CompletedFlow()
        {
            // Arrange
            var externalUserId = new ExternalUserId(Guid.NewGuid());

            var userIdentityRepository = new Mock<IUserIdentityRepository>();
            userIdentityRepository
                .Setup(e => e.FindIdentityReadyForOpenIdSetupAsync(externalUserId))
                .ReturnsAsync(GetUserIdentity);

            var userIdentityOpenIdLinkService = new UserIdentityOpenIdLinkService(
                Mock.Of<IUserRepository>(),
                userIdentityRepository.Object);

            var userIdentity = await userIdentityOpenIdLinkService.ValidateAndSetupOpenIdAsync(externalUserId).ConfigureAwait(false);

            // Act + Assert
            Assert.NotNull(userIdentity);
        }

        private static UserIdentity GetUserIdentity()
        {
            return new UserIdentity(
                new ExternalUserId(Guid.NewGuid()),
                new MockedEmailAddress(),
                UserStatus.Active,
                "fake_value",
                "fake_value",
                null,
                DateTime.UtcNow,
                AuthenticationMethod.Undetermined,
                new List<LoginIdentity>() { new("federated", "issuer", "issuerAssignedId") });
        }
    }
}
