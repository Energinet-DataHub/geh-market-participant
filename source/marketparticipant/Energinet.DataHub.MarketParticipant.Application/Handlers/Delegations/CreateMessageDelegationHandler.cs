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
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Delegations;
using Energinet.DataHub.MarketParticipant.Domain;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Delegations;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services.Rules;
using MediatR;
using NodaTime;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Delegations
{
    public sealed class CreateMessageDelegationHandler(
        IActorRepository actorRepository,
        IMessageDelegationRepository messageDelegationRepository,
        IUnitOfWorkProvider unitOfWorkProvider,
        IEntityLock entityLock,
        IAllowedMarketRoleCombinationsForDelegationRuleService allowedMarketRoleCombinationsForDelegationRuleService)
        : IRequestHandler<CreateMessageDelegationCommand>
    {
        public async Task Handle(CreateMessageDelegationCommand request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));

            var actor = await actorRepository
                .GetAsync(new(request.CreateDelegation.DelegatedFrom))
                .ConfigureAwait(false);

            var actorDelegatedTo = await actorRepository
                .GetAsync(new(request.CreateDelegation.DelegatedTo))
                .ConfigureAwait(false);

            NotFoundValidationException.ThrowIfNull(actor, request.CreateDelegation.DelegatedFrom);
            NotFoundValidationException.ThrowIfNull(actorDelegatedTo, request.CreateDelegation.DelegatedTo);

            if (actor.Status != ActorStatus.Active || actorDelegatedTo.Status != ActorStatus.Active)
            {
                throw new ValidationException("Actors to delegate from/to must both be active to delegate messages.")
                    .WithErrorCode("message_delegation.actors_from_or_to_inactive");
            }

            var uow = await unitOfWorkProvider
                .NewUnitOfWorkAsync()
                .ConfigureAwait(false);

            await using (uow.ConfigureAwait(false))
            {
                await entityLock.LockAsync(LockableEntity.Actor).ConfigureAwait(false);
                foreach (var messageType in request.CreateDelegation.MessageTypes)
                {
                    var messageDelegation = await messageDelegationRepository
                        .GetForActorAsync(actor.Id, messageType).ConfigureAwait(false) ?? new MessageDelegation(actor, messageType);

                    foreach (var gridAreaId in request.CreateDelegation.GridAreas)
                    {
                        messageDelegation.DelegateTo(
                            actorDelegatedTo.Id,
                            new(gridAreaId),
                            Instant.FromDateTimeOffset(request.CreateDelegation.StartsAt));
                    }

                    await allowedMarketRoleCombinationsForDelegationRuleService.ValidateAsync(messageDelegation).ConfigureAwait(false);
                    await messageDelegationRepository.AddOrUpdateAsync(messageDelegation).ConfigureAwait(false);
                }

                await uow.CommitAsync().ConfigureAwait(false);
            }
        }
    }
}