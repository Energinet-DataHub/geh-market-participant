﻿// Copyright 2020 Energinet DataHub A/S
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

using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users.Authentication;

namespace Energinet.DataHub.MarketParticipant.Domain.Services.ActiveDirectory;

/// <summary>
/// Manages authentication methods for the specified user.
/// </summary>
public interface IUserIdentityAuthenticationService
{
    /// <summary>
    /// Adds the specified authentication method to the given user.
    /// </summary>
    /// <param name="userId">The external id of the user to add authentication method to.</param>
    /// <param name="authenticationMethod">The authentication method to add.</param>
    Task AddAuthenticationAsync(ExternalUserId userId, AuthenticationMethod authenticationMethod);

    /// <summary>
    /// Removes all software 2FA authentication methods for the given user.
    /// </summary>
    /// <param name="userId">The external id of the user.</param>
    Task RemoveAllSoftwareTwoFactorAuthenticationMethodsAsync(ExternalUserId userId);

    /// <summary>
    /// Checks whether user has two factor authentication.
    /// </summary>
    /// <param name="userId">The external id of the user.</param>
    Task<bool> HasTwoFactorAuthenticationAsync(ExternalUserId userId);
}
