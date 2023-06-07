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
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Services;

[UnitTest]
public sealed class UserStatusCalculatorTests
{
    public static readonly object[][] TestData =
    {
        new object[] { UserStatus.Active, DateTimeOffset.UtcNow.AddDays(1), UserStatus.Invited },
        new object[] { UserStatus.Active, DateTimeOffset.UtcNow.AddDays(-1), UserStatus.InviteExpired },
        new object[] { UserStatus.Active, DateTimeOffset.UtcNow.AddDays(1), UserStatus.Invited },
        new object[] { UserStatus.Inactive, DateTimeOffset.UtcNow.AddDays(-1), UserStatus.InviteExpired },
        new object[] { UserStatus.Inactive, default(DateTimeOffset), UserStatus.Inactive },
        new object[] { UserStatus.Active, default(DateTimeOffset), UserStatus.Active }
    };

    [Theory]
    [MemberData(nameof(TestData))]
    public void CalculateUserStatus(UserStatus currentUserStatus, DateTimeOffset? invitationExpiresAt, UserStatus expected)
    {
        // arrange
        var target = new UserStatusCalculator();
        if (invitationExpiresAt.Equals(default(DateTimeOffset)))
        {
            invitationExpiresAt = null;
        }

        // act
        var actual = target.CalculateUserStatus(currentUserStatus, invitationExpiresAt);

        // assert
        Assert.Equal(expected, actual);
    }
}
