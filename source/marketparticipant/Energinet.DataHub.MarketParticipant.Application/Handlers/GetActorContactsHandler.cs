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
using Energinet.DataHub.MarketParticipant.Application.Commands.Contact;
using Energinet.DataHub.MarketParticipant.Application.Mappers;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers
{
    public sealed class GetActorContactsHandler : IRequestHandler<GetActorContactsCommand, GetActorContactsResponse>
    {
        private readonly IActorRepository _actorRepository;
        private readonly IActorContactRepository _contactRepository;

        public GetActorContactsHandler(
            IActorRepository actorRepository,
            IActorContactRepository contactRepository)
        {
            _actorRepository = actorRepository;
            _contactRepository = contactRepository;
        }

        public async Task<GetActorContactsResponse> Handle(GetActorContactsCommand request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));

            var actor = await _actorRepository
                .GetAsync(new ActorId(request.ActorId))
                .ConfigureAwait(false);

            NotFoundValidationException.ThrowIfNull(actor, request.ActorId);

            var contacts = await _contactRepository
                .GetAsync(actor.Id)
                .ConfigureAwait(false);

            return new GetActorContactsResponse(contacts.Select(ActorContactMapper.Map));
        }
    }
}
