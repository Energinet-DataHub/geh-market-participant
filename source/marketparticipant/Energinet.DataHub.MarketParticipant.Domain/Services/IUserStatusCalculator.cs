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

namespace Energinet.DataHub.MarketParticipant.Domain.Services;

public interface IUserStatusCalculator
{
    /// <summary>
    /// Calculates UserStatus from the given user and user identity.
    /// </summary>
    /// <param name="user">The user to calculate the status for.</param>
    /// <param name="userIdentity">The user identity of the user.</param>
    UserStatus CalculateUserStatus(User user, UserIdentity userIdentity)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(userIdentity);

        return CalculateUserStatus(userIdentity.Status, user.InvitationExpiresAt);
    }

    /// <summary>
    /// Calculates UserStatus from current state and user invitation.
    /// </summary>
    /// <param name="currentUserIdentityStatus">The user identity status of a user.</param>
    /// <param name="invitationExpiresAt">The expiration date of the invitation.</param>
    UserStatus CalculateUserStatus(UserIdentityStatus currentUserIdentityStatus, DateTimeOffset? invitationExpiresAt);
}
