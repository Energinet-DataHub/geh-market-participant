﻿// Copyright 2020 Energinet DataHub A/S
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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Delegations;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Delegations;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Delegations
{
    public sealed class CreateDelegationForActorHandler : IRequestHandler<CreateActorDelegationCommand, CreateActorDelegationResponse>
    {
        private readonly IActorRepository _actorRepository;

        public CreateDelegationForActorHandler(IActorRepository actorRepository)
        {
            _actorRepository = actorRepository;
        }

        public async Task<CreateActorDelegationResponse> Handle(CreateActorDelegationCommand request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));

            var actor = await _actorRepository
                .GetAsync(new ActorId(request.CreateDelegation.DelegatedFrom))
                .ConfigureAwait(false);

            var actorDelegatedTo = await _actorRepository
                .GetAsync(new ActorId(request.CreateDelegation.DelegatedTo))
                .ConfigureAwait(false);

            NotFoundValidationException.ThrowIfNull(actor, request.CreateDelegation.DelegatedFrom);
            NotFoundValidationException.ThrowIfNull(actorDelegatedTo, request.CreateDelegation.DelegatedTo);

            // TODO: Implement logic for creating delegation and then update values here.
            return new CreateActorDelegationResponse(new ActorDelegationDto(
                new ActorDelegationId(Guid.NewGuid()),
                new ActorId(request.CreateDelegation.DelegatedFrom),
                new ActorId(request.CreateDelegation.DelegatedTo),
                new List<GridAreaCode>(),
                DelegationMessageType.RSM012Inbound,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow));
        }
    }
}