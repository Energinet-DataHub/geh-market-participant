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
using Energinet.DataHub.MarketParticipant.Domain;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Events;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;
using NodaTime.Extensions;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Actors;

public sealed class ScheduleConsolidateActorsHandler : IRequestHandler<ScheduleConsolidateActorsCommand>
{
    private readonly IActorConsolidationRepository _actorConsolidationRepository;
    private readonly IDomainEventRepository _domainEventRepository;
    private readonly IActorRepository _actorRepository;
    private readonly IUnitOfWorkProvider _unitOfWorkProvider;

    public ScheduleConsolidateActorsHandler(
        IActorConsolidationRepository actorConsolidationRepository,
        IDomainEventRepository domainEventRepository,
        IUnitOfWorkProvider unitOfWorkProvider,
        IActorRepository actorRepository)
    {
        _actorConsolidationRepository = actorConsolidationRepository;
        _domainEventRepository = domainEventRepository;
        _unitOfWorkProvider = unitOfWorkProvider;
        _actorRepository = actorRepository;
    }

    public async Task Handle(ScheduleConsolidateActorsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var uow = await _unitOfWorkProvider
            .NewUnitOfWorkAsync()
            .ConfigureAwait(false);

        var allActors = (await _actorRepository
                .GetActorsAsync()
                .ConfigureAwait(false))
            .ToList();

        var notificationTargets = allActors
            .Where(actor => actor is
            {
                Status: ActorStatus.Active,
                MarketRole.Function: EicFunction.DataHubAdministrator,
            })
            .Select(actor => actor.Id)
            .ToList();

        await using (uow.ConfigureAwait(false))
        {
            await _actorConsolidationRepository
                .AddAsync(new ActorConsolidation(
                    new ActorId(request.FromActorId),
                    new ActorId(request.ToActorId),
                    request.ScheduledAt.ToInstant()))
                .ConfigureAwait(false);

            foreach (var notificationTarget in notificationTargets)
            {
                await _domainEventRepository
                    .EnqueueAsync(new ActorConsolidationScheduled(
                        notificationTarget,
                        new ActorId(request.ToActorId)))
                    .ConfigureAwait(false);
            }

            await uow.CommitAsync().ConfigureAwait(false);
        }
    }
}
