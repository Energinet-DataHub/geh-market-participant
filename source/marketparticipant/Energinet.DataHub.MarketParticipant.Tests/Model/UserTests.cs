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
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Model;

[UnitTest]
public sealed class UserTests
{
    [Fact]
    public void ValidLogonRequirements_NoExpiration_AreValid()
    {
        // Arrange + Act
        var user = new User(
            new UserId(Guid.Empty),
            new ActorId(Guid.Empty),
            new ExternalUserId(Guid.Empty),
            Array.Empty<UserRoleAssignment>(),
            null,
            null);

        // Assert
        Assert.True(user.ValidLogonRequirements);
    }

    [Fact]
    public void ValidLogonRequirements_UnderExpiration_AreValid()
    {
        // Arrange + Act
        var user = new User(
            new UserId(Guid.Empty),
            new ActorId(Guid.Empty),
            new ExternalUserId(Guid.Empty),
            Array.Empty<UserRoleAssignment>(),
            null,
            DateTimeOffset.UtcNow.AddMinutes(+20));

        // Assert
        Assert.True(user.ValidLogonRequirements);
    }

    [Fact]
    public void ValidLogonRequirements_Expired_AreInvalid()
    {
        // Arrange + Act
        var user = new User(
            new UserId(Guid.Empty),
            new ActorId(Guid.Empty),
            new ExternalUserId(Guid.Empty),
            Array.Empty<UserRoleAssignment>(),
            null,
            DateTimeOffset.UtcNow.AddMinutes(-20));

        // Assert
        Assert.False(user.ValidLogonRequirements);
    }

    [Fact]
    public void InitiateMitIdSignup_Enabled_DateSet()
    {
        // Arrange
        var user = new User(
            new UserId(Guid.Empty),
            new ActorId(Guid.Empty),
            new ExternalUserId(Guid.Empty),
            Array.Empty<UserRoleAssignment>(),
            null,
            null);

        Assert.Null(user.MitIdSignupInitiatedAt);

        // Act
        user.InitiateMitIdSignup();

        // Assert
        Assert.NotNull(user.MitIdSignupInitiatedAt);
    }

    [Fact]
    public void ActivateUserExpiration_Enabled_ExpirationSet()
    {
        // Arrange
        var user = new User(
            new UserId(Guid.Empty),
            new ActorId(Guid.Empty),
            new ExternalUserId(Guid.Empty),
            Array.Empty<UserRoleAssignment>(),
            null,
            null);

        Assert.Null(user.InvitationExpiresAt);

        // Act
        user.ActivateUserExpiration();

        // Assert
        Assert.NotNull(user.InvitationExpiresAt);
    }

    [Fact]
    public void DeactivateUserExpiration_Enabled_ExpirationCleared()
    {
        // Arrange
        var user = new User(
            new UserId(Guid.Empty),
            new ActorId(Guid.Empty),
            new ExternalUserId(Guid.Empty),
            Array.Empty<UserRoleAssignment>(),
            null,
            DateTimeOffset.UtcNow);

        Assert.NotNull(user.InvitationExpiresAt);

        // Act
        user.DeactivateUserExpiration();

        // Assert
        Assert.Null(user.InvitationExpiresAt);
    }
}
