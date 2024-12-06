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
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Actors;

public sealed class GetActorConsolidationsHandler : IRequestHandler<GetActorConsolidationsCommand, GetActorConsolidationsResponse>
{
    private readonly IActorConsolidationRepository _actorConsolidationRepository;

    public GetActorConsolidationsHandler(IActorConsolidationRepository actorConsolidationRepository)
    {
        _actorConsolidationRepository = actorConsolidationRepository;
    }

    public async Task<GetActorConsolidationsResponse> Handle(
        GetActorConsolidationsCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var actorConsolidations = await _actorConsolidationRepository
            .GetAsync()
            .ConfigureAwait(false);

        return new GetActorConsolidationsResponse(actorConsolidations.Select(x => new ActorConsolidationDto(
            x.ActorFromId.Value,
            x.ActorToId.Value,
            x.ConsolidateAt.ToDateTimeOffset(),
            x.Status)));
    }
}
