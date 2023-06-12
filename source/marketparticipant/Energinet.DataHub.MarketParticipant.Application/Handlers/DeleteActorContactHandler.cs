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
using Energinet.DataHub.MarketParticipant.Application.Commands.Contact;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers
{
    public sealed class DeleteActorContactHandler : IRequestHandler<DeleteActorContactCommand>
    {
        private readonly IActorRepository _actorRepository;
        private readonly IActorContactRepository _contactRepository;

        public DeleteActorContactHandler(
            IActorRepository actorRepository,
            IActorContactRepository contactRepository)
        {
            _actorRepository = actorRepository;
            _contactRepository = contactRepository;
        }

        public async Task<Unit> Handle(DeleteActorContactCommand request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));

            var actor = await _actorRepository
                .GetAsync(new ActorId(request.ActorId))
                .ConfigureAwait(false);

            NotFoundValidationException.ThrowIfNull(actor, request.ActorId);

            var contact = await _contactRepository
                .GetAsync(new ContactId(request.ContactId))
                .ConfigureAwait(false);

            if (contact == null)
            {
                return Unit.Value;
            }

            await _contactRepository
                .RemoveAsync(contact)
                .ConfigureAwait(false);

            return Unit.Value;
        }
    }
}
