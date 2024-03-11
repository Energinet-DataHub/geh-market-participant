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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Delegations;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Delegations;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;
using NodaTime.Extensions;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Delegations
{
    public sealed class CreateDelegationForActorHandler(
        IActorRepository actorRepository,
        IActorDelegationRepository actorDelegationRepository)
        : IRequestHandler<CreateActorDelegationCommand, CreateActorDelegationResponse>
    {
        public async Task<CreateActorDelegationResponse> Handle(CreateActorDelegationCommand request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));

            var actor = await actorRepository
                .GetAsync(new ActorId(request.CreateDelegation.DelegatedFrom))
                .ConfigureAwait(false);

            var actorDelegatedTo = await actorRepository
                .GetAsync(new ActorId(request.CreateDelegation.DelegatedTo))
                .ConfigureAwait(false);

            NotFoundValidationException.ThrowIfNull(actor, request.CreateDelegation.DelegatedFrom);
            NotFoundValidationException.ThrowIfNull(actorDelegatedTo, request.CreateDelegation.DelegatedTo);

            var result = new List<ActorDelegationDto>();
            foreach (var gridArea in request.CreateDelegation.GridAreas)
            {
                foreach (var messageType in request.CreateDelegation.MessageTypes)
                {
                    var delegation = new ActorDelegation(
                        new ActorId(request.CreateDelegation.DelegatedFrom),
                        new ActorId(request.CreateDelegation.DelegatedTo),
                        new GridAreaId(gridArea),
                        messageType,
                        DateTimeOffset.UtcNow.ToInstant(),
                        DateTimeOffset.UtcNow.ToInstant());

                    var delegationId = await actorDelegationRepository.AddOrUpdateAsync(delegation).ConfigureAwait(false);

                    result.Add(new ActorDelegationDto(
                        delegationId,
                        delegation.DelegatedBy,
                        delegation.DelegatedTo,
                        delegation.GridAreaId,
                        delegation.MessageType,
                        delegation.StartsAt.ToDateTimeOffset(),
                        delegation.ExpiresAt?.ToDateTimeOffset()));
                }
            }

            return new CreateActorDelegationResponse(result);
        }
    }
}
