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
using Energinet.DataHub.MarketParticipant.Domain;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Delegations;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;
using NodaTime;
using NodaTime.Extensions;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Delegations
{
    public sealed class CreateDelegationForActorHandler(
        IActorRepository actorRepository,
        IMessageDelegationRepository messageDelegationRepository,
        IUnitOfWorkProvider unitOfWorkProvider)
        : IRequestHandler<CreateActorDelegationCommand>
    {
        public async Task Handle(CreateActorDelegationCommand request, CancellationToken cancellationToken)
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

            var uow = await unitOfWorkProvider
                .NewUnitOfWorkAsync()
                .ConfigureAwait(false);

            await using (uow.ConfigureAwait(false))
            {
                foreach (var messageType in request.CreateDelegation.MessageTypes)
                {
                    var messageDelegation = await messageDelegationRepository
                        .GetForActorAsync(actor.Id, messageType).ConfigureAwait(false) ?? new MessageDelegation(actor, messageType);

                    foreach (var gridAreaId in request.CreateDelegation.GridAreas)
                    {
                        messageDelegation.DelegateTo(
                            actorDelegatedTo.Id,
                            new GridAreaId(gridAreaId),
                            Instant.FromDateTimeOffset(request.CreateDelegation.CreatedAt),
                            request.CreateDelegation.ExpiresAt.HasValue ? Instant.FromDateTimeOffset(request.CreateDelegation.ExpiresAt.GetValueOrDefault()) : null);
                    }
                }

                await uow.CommitAsync().ConfigureAwait(false);
            }
        }
    }
}
