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
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Actors;

public sealed class RemoveActorCredentialsHandler : IRequestHandler<RemoveActorCredentialsCommand>
{
    private readonly IActorRepository _actorRepository;
    private readonly IUnitOfWorkProvider _unitOfWorkProvider;
    private readonly IDomainEventRepository _domainEventRepository;
    private readonly IActorCredentialsRemovalService _actorCredentialsRemovalService;

    public RemoveActorCredentialsHandler(
        IActorRepository actorRepository,
        IUnitOfWorkProvider unitOfWorkProvider,
        IDomainEventRepository domainEventRepository,
        IActorCredentialsRemovalService actorCredentialsRemovalService)
    {
        _actorRepository = actorRepository;
        _unitOfWorkProvider = unitOfWorkProvider;
        _domainEventRepository = domainEventRepository;
        _actorCredentialsRemovalService = actorCredentialsRemovalService;
    }

    public async Task Handle(RemoveActorCredentialsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var actor = await _actorRepository
            .GetAsync(new ActorId(request.ActorId))
            .ConfigureAwait(false);

        NotFoundValidationException.ThrowIfNull(actor, request.ActorId);

        await _actorCredentialsRemovalService.RemoveActorCredentialsAsync(actor).ConfigureAwait(false);

        var uow = await _unitOfWorkProvider
            .NewUnitOfWorkAsync()
            .ConfigureAwait(false);

        await using (uow.ConfigureAwait(false))
        {
            var result = await _actorRepository
                .AddOrUpdateAsync(actor)
                .ConfigureAwait(false);

            result.ThrowOnError(ActorErrorHandler.HandleActorError);

            await _domainEventRepository
                .EnqueueAsync(actor)
                .ConfigureAwait(false);

            await uow.CommitAsync().ConfigureAwait(false);
        }
    }
}
