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
using Energinet.DataHub.MarketParticipant.Domain.Model.Users.Authentication;
using Microsoft.Graph;
using AuthenticationMethod = Energinet.DataHub.MarketParticipant.Domain.Model.Users.Authentication.AuthenticationMethod;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;

// TODO: UTs
public sealed class UserIdentityAuthenticationService : IUserIdentityAuthenticationService
{
    public void AddAuthentication(User user, AuthenticationMethod authenticationMethod)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (authenticationMethod == AuthenticationMethod.None)
            return;

        if (authenticationMethod == AuthenticationMethod.Undetermined)
            throw new ArgumentOutOfRangeException(nameof(authenticationMethod));

        var authentication = user.Authentication ?? new Authentication();

        switch (authenticationMethod)
        {
            case SmsAuthenticationMethod smsAuthenticationMethod:
                authentication.PhoneMethods ??= new AuthenticationPhoneMethodsCollectionPage();
                authentication.PhoneMethods.Add(new PhoneAuthenticationMethod
                {
                    PhoneNumber = smsAuthenticationMethod.PhoneNumber.Number,
                    PhoneType = AuthenticationPhoneType.Mobile
                });
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(authenticationMethod));
        }
    }
}
