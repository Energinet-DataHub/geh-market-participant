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

        var isValid = await _authorizationService
                .VerifySignatureAsync(restriction, request.Signature)
                .ConfigureAwait(false);

        return new VerifySignatureResponse(isValid);
    }
}
