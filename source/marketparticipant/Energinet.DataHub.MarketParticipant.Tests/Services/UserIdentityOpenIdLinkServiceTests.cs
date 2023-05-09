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
            var email = new MockedEmailAddress();

            var userIdentityRepository = new Mock<IUserIdentityRepository>();
            userIdentityRepository
                .Setup(e => e.FindIdentityReadyForOpenIdSetupAsync(externalUserId))
                .ReturnsAsync(GetUserIdentity(externalUserId, email, "federated"));

            var userToReturnFromService = GetUserIdentity(externalUserId, email, "emailAddress");
            userIdentityRepository
                .Setup(u => u.GetAsync(email))
                .ReturnsAsync(userToReturnFromService);

            var userRepository = new Mock<IUserRepository>();
            userRepository.Setup(e => e.GetAsync(externalUserId)).ReturnsAsync(GetUser(externalUserId));

            var userIdentityOpenIdLinkService = new UserIdentityOpenIdLinkService(
                userRepository.Object,
                userIdentityRepository.Object);

            // Act
            var userIdentity = await userIdentityOpenIdLinkService.ValidateAndSetupOpenIdAsync(externalUserId).ConfigureAwait(false);

            // Assert
            Assert.NotNull(userIdentity);
            userIdentityRepository.Verify(e => e.DeleteAsync(externalUserId));
            userIdentityRepository.Verify(e => e.AssignUserLoginIdentitiesAsync(userToReturnFromService));
        }

        private static UserIdentity GetUserIdentity(ExternalUserId externalUserId, MockedEmailAddress email, string signInType)
        {
            return new UserIdentity(
                externalUserId,
                email,
                UserStatus.Active,
                "fake_value",
                "fake_value",
                null,
                DateTime.UtcNow,
                AuthenticationMethod.Undetermined,
                new List<LoginIdentity>() { new(signInType, "issuer", "issuerAssignedId") });
        }

        private static User GetUser(ExternalUserId externalUserId)
        {
            var user = new User(externalUserId);
            user.InitiateMitIdSignup();
            return user;
        }
    }
}
