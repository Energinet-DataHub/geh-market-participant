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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services.Rules;

namespace Energinet.DataHub.MarketParticipant.Domain.Services;

public sealed class ActorFactoryService : IActorFactoryService
{
    private readonly IActorRepository _actorRepository;
    private readonly IUnitOfWorkProvider _unitOfWorkProvider;
    private readonly IOverlappingEicFunctionsRuleService _overlappingEicFunctionsRuleService;
    private readonly IUniqueGlobalLocationNumberRuleService _uniqueGlobalLocationNumberRuleService;
    private readonly IUniqueMarketRoleGridAreaRuleService _uniqueMarketRoleGridAreaRuleService;
    private readonly IDomainEventRepository _domainEventRepository;

    public ActorFactoryService(
        IActorRepository actorRepository,
        IUnitOfWorkProvider unitOfWorkProvider,
        IOverlappingEicFunctionsRuleService overlappingEicFunctionsRuleService,
        IUniqueGlobalLocationNumberRuleService uniqueGlobalLocationNumberRuleService,
        IUniqueMarketRoleGridAreaRuleService uniqueMarketRoleGridAreaRuleService,
        IDomainEventRepository domainEventRepository)
    {
        _actorRepository = actorRepository;
        _unitOfWorkProvider = unitOfWorkProvider;
        _overlappingEicFunctionsRuleService = overlappingEicFunctionsRuleService;
        _uniqueGlobalLocationNumberRuleService = uniqueGlobalLocationNumberRuleService;
        _uniqueMarketRoleGridAreaRuleService = uniqueMarketRoleGridAreaRuleService;
        _domainEventRepository = domainEventRepository;
    }

    public async Task<Actor> CreateAsync(
        Organization organization,
        ActorNumber actorNumber,
        ActorName actorName,
        IReadOnlyCollection<ActorMarketRole> marketRoles)
    {
        ArgumentNullException.ThrowIfNull(organization);
        ArgumentNullException.ThrowIfNull(actorNumber);
        ArgumentNullException.ThrowIfNull(actorName);
        ArgumentNullException.ThrowIfNull(marketRoles);

        var newActor = new Actor(organization.Id, actorNumber, actorName);

        foreach (var marketRole in marketRoles)
            newActor.AddMarketRole(marketRole);

        await _uniqueGlobalLocationNumberRuleService
            .ValidateGlobalLocationNumberAvailableAsync(organization, actorNumber)
            .ConfigureAwait(false);

        await _overlappingEicFunctionsRuleService
            .ValidateEicFunctionsAcrossActorsAsync(newActor)
            .ConfigureAwait(false);

        var uow = await _unitOfWorkProvider
            .NewUnitOfWorkAsync()
            .ConfigureAwait(false);

        await using (uow.ConfigureAwait(false))
        {
            var actorId = await SaveActorAsync(newActor).ConfigureAwait(false);

            var committedActor = (await _actorRepository
                .GetAsync(actorId)
                .ConfigureAwait(false))!;

            await _uniqueMarketRoleGridAreaRuleService
                .ValidateAndReserveAsync(committedActor)
                .ConfigureAwait(false);

            committedActor.Activate();

            await _domainEventRepository
                .EnqueueAsync(committedActor)
                .ConfigureAwait(false);

            await SaveActorAsync(committedActor).ConfigureAwait(false);

            await uow.CommitAsync().ConfigureAwait(false);

            return committedActor;
        }
    }

    private async Task<ActorId> SaveActorAsync(Actor newActor)
    {
        var result = await _actorRepository
            .AddOrUpdateAsync(newActor)
            .ConfigureAwait(false);

        result.ThrowOnError(ActorErrorHandler.HandleActorError);
        return result.Value;
    }
}
