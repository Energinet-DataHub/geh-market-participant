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

using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers;

public sealed class SynchronizeActorsHandler : IRequestHandler<SynchronizeActorsCommand>
{
    private readonly IUnitOfWorkProvider _unitOfWorkProvider;
    private readonly IActorRepository _actorRepository;
    private readonly IExternalActorIdConfigurationService _externalActorIdConfigurationService;
    private readonly IExternalActorSynchronizationRepository _externalActorSynchronizationRepository;
    private readonly IDomainEventRepository _domainEventRepository;

    public SynchronizeActorsHandler(
        IUnitOfWorkProvider unitOfWorkProvider,
        IActorRepository actorRepository,
        IExternalActorIdConfigurationService externalActorIdConfigurationService,
        IExternalActorSynchronizationRepository externalActorSynchronizationRepository,
        IDomainEventRepository domainEventRepository)
    {
        _unitOfWorkProvider = unitOfWorkProvider;
        _actorRepository = actorRepository;
        _externalActorIdConfigurationService = externalActorIdConfigurationService;
        _externalActorSynchronizationRepository = externalActorSynchronizationRepository;
        _domainEventRepository = domainEventRepository;
    }

    public async Task Handle(SynchronizeActorsCommand request, CancellationToken cancellationToken)
    {
        var uow = await _unitOfWorkProvider
            .NewUnitOfWorkAsync()
            .ConfigureAwait(false);

        await using (uow.ConfigureAwait(false))
        {
            var nextEntry = await _externalActorSynchronizationRepository
                 .DequeueNextAsync()
                 .ConfigureAwait(false);

            if (nextEntry.HasValue)
            {
                var actor = (await _actorRepository
                    .GetAsync(new ActorId(nextEntry.Value))
                    .ConfigureAwait(false))!;

                // TODO: This service must be replaced with a reliable version in a future PR.
                await _externalActorIdConfigurationService
                    .AssignExternalActorIdAsync(actor)
                    .ConfigureAwait(false);

                await _actorRepository
                    .AddOrUpdateAsync(actor)
                    .ConfigureAwait(false);

                await _domainEventRepository
                    .EnqueueAsync(actor)
                    .ConfigureAwait(false);
            }

            await uow.CommitAsync().ConfigureAwait(false);
        }
    }
}
