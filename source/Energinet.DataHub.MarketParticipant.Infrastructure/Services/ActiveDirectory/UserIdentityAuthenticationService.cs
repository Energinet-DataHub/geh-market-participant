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
using System.Net;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users.Authentication;
using Energinet.DataHub.MarketParticipant.Infrastructure.Model.ActiveDirectory;
using Microsoft.Graph;
using Microsoft.Graph.Models.ODataErrors;
using AuthenticationMethod = Energinet.DataHub.MarketParticipant.Domain.Model.Users.Authentication.AuthenticationMethod;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Services.ActiveDirectory;

public sealed class UserIdentityAuthenticationService : IUserIdentityAuthenticationService
{
    private readonly GraphServiceClient _graphClient;

    public UserIdentityAuthenticationService(GraphServiceClient graphClient)
    {
        _graphClient = graphClient;
    }

    public async Task AddAuthenticationAsync(ExternalUserId userId, AuthenticationMethod authenticationMethod)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(authenticationMethod);

        if (authenticationMethod == AuthenticationMethod.Undetermined)
            throw new ArgumentOutOfRangeException(nameof(authenticationMethod));

        var authenticationBuilder = _graphClient
            .Users[userId.ToString()]
            .Authentication!;

        var externalAuthenticationMethod = ChooseExternalMethod(authenticationMethod);

        try
        {
            await externalAuthenticationMethod
                .AssignAsync(authenticationBuilder)
                .ConfigureAwait(false);
        }
        catch (ODataError ex) when (ex.ResponseStatusCode == (int)HttpStatusCode.BadRequest)
        {
            // If there is a bad request with 'Mobile phone method is already registered for this account.', MFA is already configured.
            // Check if MFA is equivalent to current authentication method, then ignore the error.
            // Otherwise, the user is in an unknown state and exception is thrown.
            if (!await externalAuthenticationMethod.VerifyAsync(_graphClient, authenticationBuilder).ConfigureAwait(false))
                throw;
        }
    }

    private static IExternalAuthenticationMethod ChooseExternalMethod(AuthenticationMethod authenticationMethod)
    {
        return authenticationMethod switch
        {
            SmsAuthenticationMethod smsAuthenticationMethod => new ExternalSmsAuthenticationMethod(smsAuthenticationMethod),
            _ => throw new ArgumentOutOfRangeException(nameof(authenticationMethod))
        };
    }
}
