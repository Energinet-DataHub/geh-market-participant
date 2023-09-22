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
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Tests.Common;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Services;

[UnitTest]
public sealed class AuditIdentityResolverTests
{
    [Fact]
    public async Task Resolve_KnownIdentity_ReturnsKnownInformation()
    {
        // Arrange
        var userRepository = new Mock<IUserRepository>();
        var userIdentityRepository = new Mock<IUserIdentityRepository>();

        var target = new AuditIdentityResolver(
            userRepository.Object,
            userIdentityRepository.Object);

        var auditIdentity = KnownAuditIdentityProvider.OrganizationBackgroundService.IdentityId;

        // Act
        var actual = await target.ResolveAsync(auditIdentity);

        // Assert
        Assert.Equal("DataHub " + KnownAuditIdentityProvider.OrganizationBackgroundService.FriendlyName, actual.FullName);
    }

    [Fact]
    public async Task Resolve_UserIdentity_ReturnsUserInformation()
    {
        // Arrange
        var userRepository = new Mock<IUserRepository>();
        var userIdentityRepository = new Mock<IUserIdentityRepository>();

        var target = new AuditIdentityResolver(
            userRepository.Object,
            userIdentityRepository.Object);

        var userId = Guid.NewGuid();
        var userExternalId = Guid.NewGuid();

        var user = TestPreparationModels.MockedUser(userId, userExternalId);
        var userIdentity = TestPreparationModels.MockedUserIdentity(userExternalId);

        userRepository
            .Setup(x => x.GetAsync(new UserId(userId)))
            .ReturnsAsync(user);

        userIdentityRepository
            .Setup(x => x.GetAsync(new ExternalUserId(userExternalId)))
            .ReturnsAsync(userIdentity);

        var auditIdentity = new AuditIdentity(userId);

        // Act
        var actual = await target.ResolveAsync(auditIdentity);

        // Assert
        Assert.Equal(userIdentity.FullName, actual.FullName);
    }
}
