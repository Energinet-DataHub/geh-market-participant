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
using Energinet.DataHub.MarketParticipant.Application.Commands.Actors;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Events;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Actors;

public sealed class ValidateActorCredentialsHandler : IRequestHandler<ValidateActorCredentialsCommand>
{
    private readonly IActorRepository _actorRepository;
    private readonly IDomainEventRepository _domainEventRepository;

    public ValidateActorCredentialsHandler(
        IActorRepository actorRepository,
        IDomainEventRepository domainEventRepository)
    {
        _actorRepository = actorRepository;
        _domainEventRepository = domainEventRepository;
    }

    public async Task Handle(ValidateActorCredentialsCommand request, CancellationToken cancellationToken)
    {
        var allActors = await _actorRepository
            .GetActorsAsync()
            .ConfigureAwait(false);

        foreach (var actor in allActors)
        {
            if (actor.Status != ActorStatus.Active || actor.Credentials == null)
                continue;

            if (actor.Credentials.ExpiresSoon())
            {
                await _domainEventRepository
                    .EnqueueAsync(new ActorCredentialsExpiring(actor.Id, actor.Id))
                    .ConfigureAwait(false);
            }
        }
    }
}
