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
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers;

public sealed class UpdateActorHandler : IRequestHandler<UpdateActorCommand>
{
    private readonly IActorRepository _actorRepository;

    public UpdateActorHandler(IActorRepository actorRepository)
    {
        _actorRepository = actorRepository;
    }

    public async Task<Unit> Handle(UpdateActorCommand request, CancellationToken cancellationToken)
    {
        var actor = await _actorRepository.GetAsync(new ActorId(request.ActorId)).ConfigureAwait(false);

        if (actor == null)
        {
            throw new NotFoundException($"Actor with id {request.ActorId} not found.");
        }

        actor.Name = new ActorName(request.Name.Value);
        actor.MarketRole = new ActorMarketRole(request.MarketRole.EicFunction, request.MarketRole.Comment);

        await _actorRepository.AddOrUpdateAsync(actor).ConfigureAwait(false);

        return Unit.Value;
    }
}
