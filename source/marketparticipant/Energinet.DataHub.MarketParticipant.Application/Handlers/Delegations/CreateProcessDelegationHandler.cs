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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Delegations;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Delegations;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services.Rules;
using MediatR;
using NodaTime;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Delegations;

public sealed class CreateProcessDelegationHandler : IRequestHandler<CreateProcessDelegationCommand>
{
    private readonly IActorRepository _actorRepository;
    private readonly IProcessDelegationRepository _processDelegationRepository;
    private readonly IDomainEventRepository _domainEventRepository;
    private readonly IUnitOfWorkProvider _unitOfWorkProvider;
    private readonly IEntityLock _entityLock;
    private readonly IAllowedMarketRoleCombinationsForDelegationRuleService _allowedMarketRoleCombinationsForDelegationRuleService;
    private readonly IProcessDelegationHelperService _processDelegationHelperService;

    public CreateProcessDelegationHandler(
        IActorRepository actorRepository,
        IProcessDelegationRepository processDelegationRepository,
        IDomainEventRepository domainEventRepository,
        IUnitOfWorkProvider unitOfWorkProvider,
        IEntityLock entityLock,
        IAllowedMarketRoleCombinationsForDelegationRuleService allowedMarketRoleCombinationsForDelegationRuleService,
        IProcessDelegationHelperService processDelegationHelperService)
    {
        _actorRepository = actorRepository;
        _processDelegationRepository = processDelegationRepository;
        _domainEventRepository = domainEventRepository;
        _unitOfWorkProvider = unitOfWorkProvider;
        _entityLock = entityLock;
        _allowedMarketRoleCombinationsForDelegationRuleService = allowedMarketRoleCombinationsForDelegationRuleService;
        _processDelegationHelperService = processDelegationHelperService;
    }

    public async Task Handle(CreateProcessDelegationCommand request, CancellationToken cancellationToken)
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
                .WithErrorCode("process_delegation.actors_from_or_to_inactive");
        }

        var uow = await _unitOfWorkProvider
            .NewUnitOfWorkAsync()
            .ConfigureAwait(false);

        await using (uow.ConfigureAwait(false))
        {
            await _entityLock
                .LockAsync(LockableEntity.Actor)
                .ConfigureAwait(false);

            await _processDelegationHelperService
                .VerifyValidActorsForProcessDelegationAsync(
                    actor,
                    actorDelegatedTo,
                    request.CreateDelegation.DelegatedProcesses,
                    request.CreateDelegation.GridAreas)
                .ConfigureAwait(false);

            foreach (var process in request.CreateDelegation.DelegatedProcesses)
            {
                var processDelegation = await _processDelegationRepository
                    .GetForActorAsync(actor.Id, process)
                    .ConfigureAwait(false) ?? new ProcessDelegation(actor, process);

                foreach (var gridAreaId in request.CreateDelegation.GridAreas)
                {
                    processDelegation.DelegateTo(
                        actorDelegatedTo.Id,
                        new(gridAreaId),
                        Instant.FromDateTimeOffset(request.CreateDelegation.StartsAt));
                }

                await _allowedMarketRoleCombinationsForDelegationRuleService
                    .ValidateAsync(processDelegation)
                    .ConfigureAwait(false);

                var processDelegationId = await _processDelegationRepository
                    .AddOrUpdateAsync(processDelegation)
                    .ConfigureAwait(false);

                await _domainEventRepository
                    .EnqueueAsync(processDelegation, processDelegationId.Value)
                    .ConfigureAwait(false);
            }

            await uow.CommitAsync().ConfigureAwait(false);
        }
    }
}
