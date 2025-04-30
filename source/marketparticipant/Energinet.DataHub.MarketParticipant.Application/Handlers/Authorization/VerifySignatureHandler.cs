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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Authorization;
using Energinet.DataHub.MarketParticipant.Authorization;
using Energinet.DataHub.MarketParticipant.Authorization.Restriction;
using Energinet.DataHub.MarketParticipant.Authorization.Services;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Authorization;

public sealed class VerifySignatureHandler
    : IRequestHandler<VerifySignatureCommand, VerifySignatureResponse>
{
    private readonly IAuthorizationService _authorizationService;

    public VerifySignatureHandler(
        IAuthorizationService authorizationService)
    {
        _authorizationService = authorizationService;
    }

    public async Task<VerifySignatureResponse> Handle(
        VerifySignatureCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var restriction = new AuthorizationRestriction();

        bool result = false;
        if (IsStringBase64(request.Signature))
        {
            var conversionResult = Convert.FromBase64String(request.Signature);

            result = await _authorizationService
                    .VerifySignatureAsync(restriction, conversionResult)
                    .ConfigureAwait(false);
        }

        return new VerifySignatureResponse(result);
    }

    private static bool IsStringBase64(string signature)
    {
        try
        {
            var conversionResult = Convert.FromBase64String(signature);
            return true;
        }
#pragma warning disable CA1031
        catch (Exception)
        {
            return false;
        }
    }
}
