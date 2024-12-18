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
using Energinet.DataHub.MarketParticipant.Application.Commands.Actors;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.RevisionLog.Integration;
using MediatR;
using NodaTime;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Actors;

public sealed class ConsolidateActorsHandler : IRequestHandler<ConsolidateActorsCommand>
{
    private readonly IActorConsolidationRepository _actorConsolidationRepository;
    private readonly IActorConsolidationService _actorConsolidationService;
    private readonly IRevisionLogClient _revisionLogClient;

    public ConsolidateActorsHandler(
        IActorConsolidationRepository actorConsolidationRepository,
        IActorConsolidationService actorConsolidationService,
        IRevisionLogClient revisionLogClient)
    {
        _actorConsolidationRepository = actorConsolidationRepository;
        _actorConsolidationService = actorConsolidationService;
        _revisionLogClient = revisionLogClient;
    }

    public async Task Handle(ConsolidateActorsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var actorsReadyToConsolidate = await _actorConsolidationRepository
            .GetReadyToConsolidateAsync()
            .ConfigureAwait(false);

        foreach (var actorConsolidation in actorsReadyToConsolidate)
        {
            await _revisionLogClient
                .LogAsync(new RevisionLogEntry(
                    logId: Guid.NewGuid(),
                    systemId: SubsystemInformation.Id,
                    activity: "ConsolidateActor",
                    occurredOn: SystemClock.Instance.GetCurrentInstant(),
                    origin: nameof(ConsolidateActorsHandler),
                    affectedEntityType: nameof(Actor),
                    affectedEntityKey: actorConsolidation.ActorFromId.ToString(),
                    payload: actorConsolidation.Id.ToString()))
                .ConfigureAwait(false);

            await _actorConsolidationService.ConsolidateAsync(actorConsolidation).ConfigureAwait(false);
        }
    }
}
