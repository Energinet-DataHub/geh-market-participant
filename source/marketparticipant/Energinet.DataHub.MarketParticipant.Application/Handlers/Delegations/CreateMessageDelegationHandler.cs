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
    public sealed class CreateMessageDelegationHandler : IRequestHandler<CreateMessageDelegationCommand>
    {
        private readonly IActorRepository _actorRepository;
        private readonly IMessageDelegationRepository _messageDelegationRepository;
        private readonly IDomainEventRepository _domainEventRepository;
        private readonly IUnitOfWorkProvider _unitOfWorkProvider;
        private readonly IEntityLock _entityLock;
        private readonly IAllowedMarketRoleCombinationsForDelegationRuleService _allowedMarketRoleCombinationsForDelegationRuleService;

        public CreateMessageDelegationHandler(
            IActorRepository actorRepository,
            IMessageDelegationRepository messageDelegationRepository,
            IDomainEventRepository domainEventRepository,
            IUnitOfWorkProvider unitOfWorkProvider,
            IEntityLock entityLock,
            IAllowedMarketRoleCombinationsForDelegationRuleService allowedMarketRoleCombinationsForDelegationRuleService)
        {
            _actorRepository = actorRepository;
            _messageDelegationRepository = messageDelegationRepository;
            _domainEventRepository = domainEventRepository;
            _unitOfWorkProvider = unitOfWorkProvider;
            _entityLock = entityLock;
            _allowedMarketRoleCombinationsForDelegationRuleService = allowedMarketRoleCombinationsForDelegationRuleService;
        }

        public async Task Handle(CreateMessageDelegationCommand request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var actor = await _actorRepository
                .GetAsync(new(request.CreateDelegation.DelegatedFrom))
                .ConfigureAwait(false);

            NotFoundValidationException.ThrowIfNull(actor, request.CreateDelegation.DelegatedFrom);

            var actorDelegatedTo = await _actorRepository
                .GetAsync(new(request.CreateDelegation.DelegatedTo))
                .ConfigureAwait(false);

            NotFoundValidationException.ThrowIfNull(actorDelegatedTo, request.CreateDelegation.DelegatedTo);

            if (actor.Status != ActorStatus.Active || actorDelegatedTo.Status != ActorStatus.Active)
            {
                throw new ValidationException("Actors to delegate from/to must both be active to delegate messages.")
                    .WithErrorCode("message_delegation.actors_from_or_to_inactive");
            }

            var uow = await _unitOfWorkProvider
                .NewUnitOfWorkAsync()
                .ConfigureAwait(false);

            await using (uow.ConfigureAwait(false))
            {
                await _entityLock
                    .LockAsync(LockableEntity.Actor)
                    .ConfigureAwait(false);

                foreach (var messageType in request.CreateDelegation.MessageTypes)
                {
                    var messageDelegation = await EnsureMessageDelegationAsync(actor, messageType).ConfigureAwait(false);

                    foreach (var gridAreaId in request.CreateDelegation.GridAreas)
                    {
                        messageDelegation.DelegateTo(
                            actorDelegatedTo.Id,
                            new(gridAreaId),
                            Instant.FromDateTimeOffset(request.CreateDelegation.StartsAt));
                    }

                    await _allowedMarketRoleCombinationsForDelegationRuleService
                        .ValidateAsync(messageDelegation)
                        .ConfigureAwait(false);

                    await _domainEventRepository
                        .EnqueueAsync(messageDelegation)
                        .ConfigureAwait(false);

                    await _messageDelegationRepository
                        .AddOrUpdateAsync(messageDelegation)
                        .ConfigureAwait(false);
                }

                await uow.CommitAsync().ConfigureAwait(false);
            }
        }

        private async Task<MessageDelegation> EnsureMessageDelegationAsync(Domain.Model.Actor actor, DelegationMessageType messageType)
        {
            var messageDelegation = await _messageDelegationRepository
                .GetForActorAsync(actor.Id, messageType)
                .ConfigureAwait(false);

            if (messageDelegation == null)
            {
                var messageDelegationId = await _messageDelegationRepository
                    .AddOrUpdateAsync(new MessageDelegation(actor, messageType))
                    .ConfigureAwait(false);

                messageDelegation = await _messageDelegationRepository
                    .GetAsync(messageDelegationId)
                    .ConfigureAwait(false);
            }

            return messageDelegation!;
        }
    }
}
