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
using Energinet.DataHub.MarketParticipant.Application.Commands.User;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.User;

public class GetAssociatedUserActorsHandler : IRequestHandler<GetAssociatedUserActorsCommand, GetAssociatedUserActorsResponse>
{
    public Task<GetAssociatedUserActorsResponse> Handle(
        GetAssociatedUserActorsCommand request,
        CancellationToken cancellationToken)
    {
        var associatedActors = new[]
        {
            Guid.Parse("091C180F-2230-42C8-8209-7C134926BF2E"),
            Guid.Parse("3143B0D4-C3E0-4727-8266-EC0A03B0B356")
        };

        return Task.FromResult(new GetAssociatedUserActorsResponse(associatedActors));
    }
}
