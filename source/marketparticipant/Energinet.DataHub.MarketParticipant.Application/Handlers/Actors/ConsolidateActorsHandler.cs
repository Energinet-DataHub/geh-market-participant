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
using Energinet.DataHub.MarketParticipant.Domain;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Actors;

public sealed class ConsolidateActorsHandler : IRequestHandler<ConsolidateActorsCommand>
{
    private readonly IActorConsolidationRepository _actorConsolidationRepository;
    private readonly IDomainEventRepository _domainEventRepository;
    private readonly IUnitOfWorkProvider _unitOfWorkProvider;
    private readonly IActorConsolidationService _actorConsolidationService;

    public ConsolidateActorsHandler(
        IActorConsolidationRepository actorConsolidationRepository,
        IDomainEventRepository domainEventRepository,
        IUnitOfWorkProvider unitOfWorkProvider,
        IActorConsolidationService actorConsolidationService)
    {
        _actorConsolidationRepository = actorConsolidationRepository;
        _domainEventRepository = domainEventRepository;
        _unitOfWorkProvider = unitOfWorkProvider;
        _actorConsolidationService = actorConsolidationService;
    }

    public async Task Handle(ConsolidateActorsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var uow = await _unitOfWorkProvider
            .NewUnitOfWorkAsync()
            .ConfigureAwait(false);

        await using (uow.ConfigureAwait(false))
        {
            var actorsReadyToConsolidate = await _actorConsolidationRepository
                .GetReadyToConsolidateAsync()
                .ConfigureAwait(false);

            foreach (var actorConsolidation in actorsReadyToConsolidate)
            {
                await _actorConsolidationService.ConsolidateAsync(actorConsolidation).ConfigureAwait(false);
            }

            await uow.CommitAsync().ConfigureAwait(false);
        }
    }
}
