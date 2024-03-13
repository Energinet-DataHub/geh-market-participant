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
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Delegations
{
    public sealed class GetDelegationsForActorHandler(
        IActorRepository actorRepository,
        IMessageDelegationRepository messageDelegationRepository)
        : IRequestHandler<GetDelegationsForActorCommand, GetDelegationsForActorResponse>
    {
        public async Task<GetDelegationsForActorResponse> Handle(GetDelegationsForActorCommand request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));

            var actor = await actorRepository
                .GetAsync(new ActorId(request.ActorId))
                .ConfigureAwait(false);

            NotFoundValidationException.ThrowIfNull(actor, request.ActorId);

            var delegations = await messageDelegationRepository
                .GetForActorAsync(new ActorId(request.ActorId))
                .ConfigureAwait(false);

            var result = new List<MessageDelegationDto>();
            foreach (var messageDelegation in delegations)
            {
                var delegationPeriods = messageDelegation.Delegations.Select(x =>
                    new MessageDelegationPeriodDto(
                        x.Id,
                        x.DelegatedTo,
                        x.GridAreaId,
                        x.StartsAt.ToDateTimeOffset(),
                        x.StopsAt?.ToDateTimeOffset()));

                var messageDelegationDto = new MessageDelegationDto(
                    messageDelegation.Id,
                    messageDelegation.DelegatedBy,
                    messageDelegation.MessageType,
                    delegationPeriods);

                result.Add(messageDelegationDto);
            }

            return new GetDelegationsForActorResponse(result);
        }
    }
}
