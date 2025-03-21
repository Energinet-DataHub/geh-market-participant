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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Actors;
using Energinet.DataHub.MarketParticipant.Application.Mappers;
using Energinet.DataHub.MarketParticipant.Domain;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services.Rules;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Actors;

public sealed class UpdateActorHandler : IRequestHandler<UpdateActorCommand>
{
    private readonly IActorRepository _actorRepository;
    private readonly IUnitOfWorkProvider _unitOfWorkProvider;
    private readonly IOverlappingEicFunctionsRuleService _overlappingEicFunctionsRuleService;
    private readonly IUniqueMarketRoleGridAreaRuleService _uniqueMarketRoleGridAreaRuleService;
    private readonly IDomainEventRepository _domainEventRepository;

    public UpdateActorHandler(
        IActorRepository actorRepository,
        IUnitOfWorkProvider unitOfWorkProvider,
        IOverlappingEicFunctionsRuleService overlappingEicFunctionsRuleService,
        IUniqueMarketRoleGridAreaRuleService uniqueMarketRoleGridAreaRuleService,
        IDomainEventRepository domainEventRepository)
    {
        _actorRepository = actorRepository;
        _unitOfWorkProvider = unitOfWorkProvider;
        _overlappingEicFunctionsRuleService = overlappingEicFunctionsRuleService;
        _uniqueMarketRoleGridAreaRuleService = uniqueMarketRoleGridAreaRuleService;
        _domainEventRepository = domainEventRepository;
    }

    public async Task Handle(UpdateActorCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var actor = await _actorRepository
            .GetAsync(new ActorId(request.ActorId))
            .ConfigureAwait(false);

        NotFoundValidationException.ThrowIfNull(actor, request.ActorId);

        UpdateAggregate(actor, request.ChangeActor);
        await ValidateAggregateAsync(actor).ConfigureAwait(false);

        var uow = await _unitOfWorkProvider
            .NewUnitOfWorkAsync()
            .ConfigureAwait(false);

        await using (uow.ConfigureAwait(false))
        {
            await _actorRepository
                .AddOrUpdateAsync(actor)
                .ConfigureAwait(false);

            await _domainEventRepository
                .EnqueueAsync(actor)
                .ConfigureAwait(false);

            await uow.CommitAsync().ConfigureAwait(false);
        }
    }

    private static void UpdateAggregate(Actor actor, ChangeActorDto changes)
    {
        if (actor.Status == ActorStatus.New)
        {
            if (changes.MarketRole is { } mr)
            {
                actor.UpdateMarketRole(MarketRoleMapper.Map(mr));
            }
        }

        actor.Name = new ActorName(changes.Name.Value);
        actor.Status = Enum.Parse<ActorStatus>(changes.Status, true);
    }

    private async Task ValidateAggregateAsync(Actor actor)
    {
        await _uniqueMarketRoleGridAreaRuleService
            .ValidateAndReserveAsync(actor)
            .ConfigureAwait(false);

        await _overlappingEicFunctionsRuleService
            .ValidateEicFunctionsAcrossActorsAsync(actor)
            .ConfigureAwait(false);
    }
}
