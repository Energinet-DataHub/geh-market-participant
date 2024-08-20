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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Actors;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Actors;

public sealed class GetActorTokenDataHandler : IRequestHandler<GetActorTokenDataCommand, GetActorTokenDataResponse>
{
    private readonly IActorRepository _actorRepository;

    public GetActorTokenDataHandler(IActorRepository actorRepository)
    {
        _actorRepository = actorRepository;
    }

    public async Task<GetActorTokenDataResponse> Handle(GetActorTokenDataCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var actorTokenData = await _actorRepository
            .GetActorTokenDataAsync(new ActorId(request.ActorId))
            .ConfigureAwait(false);

        NotFoundValidationException.ThrowIfNull(actorTokenData, request.ActorId);

        return new GetActorTokenDataResponse(
            new ActorTokenDataDto(
                actorTokenData.ActorId,
                actorTokenData.ActorNumber,
                actorTokenData.MarketRoles.Select(x => new ActorTokenDataMarketRoleDto
                {
                    Function = x.Function,
                    GridAreas = x.GridAreas.Select(y => new ActorTokenDataGridAreaDto
                    {
                        GridAreaCode = y.GridAreaCode,
                    }),
                })));
    }
}
