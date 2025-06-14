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

using System.Net.Http.Headers;
using Azure.Core;

namespace Energinet.DataHub.MarketParticipant.Authorization.Application.Authorization;

internal sealed class AuthorizationHeaderProvider : IAuthorizationHeaderProvider
{
    private readonly TokenCredential _credential;
    private readonly string _applicationIdUri;

    public AuthorizationHeaderProvider(TokenCredential credential, string applicationIdUri)
    {
        _credential = credential;
        _applicationIdUri = applicationIdUri;
    }

    /// <inheritdoc/>
    public AuthenticationHeaderValue CreateAuthorizationHeader()
    {
        var tokenResponse = _credential.GetToken(new TokenRequestContext([_applicationIdUri]), CancellationToken.None);
        return new AuthenticationHeaderValue("Bearer", tokenResponse.Token);
    }
}
