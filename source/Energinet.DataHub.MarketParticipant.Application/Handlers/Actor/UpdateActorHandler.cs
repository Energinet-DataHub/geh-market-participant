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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Actor;
using Energinet.DataHub.MarketParticipant.Application.Helpers;
using Energinet.DataHub.MarketParticipant.Application.Mappers;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Energinet.DataHub.MarketParticipant.Domain.Services.Rules;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Actor
{
    public sealed class UpdateActorHandler : IRequestHandler<UpdateActorCommand>
    {
        private readonly IActorRepository _actorRepository;
        private readonly IUnitOfWorkProvider _unitOfWorkProvider;
        private readonly IChangesToActorHelper _changesToActorHelper;
        private readonly IActorIntegrationEventsQueueService _actorIntegrationEventsQueueService;
        private readonly IOverlappingBusinessRolesRuleService _overlappingBusinessRolesRuleService;
        private readonly IAllowedGridAreasRuleService _allowedGridAreasRuleService;
        private readonly IExternalActorSynchronizationRepository _externalActorSynchronizationRepository;
        private readonly IUniqueMarketRoleGridAreaRuleService _uniqueMarketRoleGridAreaRuleRuleService;
        private readonly ICombinationOfBusinessRolesRuleService _combinationOfBusinessRolesRuleService;
        private readonly IActorStatusMarketRolesRuleService _actorStatusMarketRolesRuleService;

        public UpdateActorHandler(
            IActorRepository actorRepository,
            IUnitOfWorkProvider unitOfWorkProvider,
            IChangesToActorHelper changesToActorHelper,
            IActorIntegrationEventsQueueService actorIntegrationEventsQueueService,
            IOverlappingBusinessRolesRuleService overlappingBusinessRolesRuleService,
            IAllowedGridAreasRuleService allowedGridAreasRuleService,
            IExternalActorSynchronizationRepository externalActorSynchronizationRepository,
            IUniqueMarketRoleGridAreaRuleService uniqueMarketRoleGridAreaRuleRuleService,
            ICombinationOfBusinessRolesRuleService combinationOfBusinessRolesRuleService,
            IActorStatusMarketRolesRuleService actorStatusMarketRolesRuleService)
        {
            _actorRepository = actorRepository;
            _unitOfWorkProvider = unitOfWorkProvider;
            _changesToActorHelper = changesToActorHelper;
            _actorIntegrationEventsQueueService = actorIntegrationEventsQueueService;
            _overlappingBusinessRolesRuleService = overlappingBusinessRolesRuleService;
            _allowedGridAreasRuleService = allowedGridAreasRuleService;
            _externalActorSynchronizationRepository = externalActorSynchronizationRepository;
            _uniqueMarketRoleGridAreaRuleRuleService = uniqueMarketRoleGridAreaRuleRuleService;
            _combinationOfBusinessRolesRuleService = combinationOfBusinessRolesRuleService;
            _actorStatusMarketRolesRuleService = actorStatusMarketRolesRuleService;
        }

        public async Task<Unit> Handle(UpdateActorCommand request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));

            var actor = await _actorRepository
                .GetAsync(new ActorId(request.ActorId))
                .ConfigureAwait(false);

            if (actor == null)
            {
                throw new NotFoundValidationException(request.ActorId);
            }

            var actorChangedIntegrationEvents = await _changesToActorHelper
                .FindChangesMadeToActorAsync(actor.OrganizationId, actor, request)
                .ConfigureAwait(false);

            UpdateActorStatus(actor, request);
            UpdateActorName(actor, request);
            await UpdateActorMarketRolesAndChildrenAsync(actor, request).ConfigureAwait(false);

            await _uniqueMarketRoleGridAreaRuleRuleService.ValidateAsync(actor).ConfigureAwait(false);
            await _actorStatusMarketRolesRuleService.ValidateAsync(actor).ConfigureAwait(false);

            var uow = await _unitOfWorkProvider
                .NewUnitOfWorkAsync()
                .ConfigureAwait(false);

            await using (uow.ConfigureAwait(false))
            {
                await _actorRepository
                    .AddOrUpdateAsync(actor)
                    .ConfigureAwait(false);

                await _externalActorSynchronizationRepository
                    .ScheduleAsync(actor.Id.Value)
                    .ConfigureAwait(false);

                await _actorIntegrationEventsQueueService
                    .EnqueueActorUpdatedEventAsync(actor)
                    .ConfigureAwait(false);

                await _actorIntegrationEventsQueueService
                    .EnqueueActorUpdatedEventAsync(actor.Id, actorChangedIntegrationEvents)
                    .ConfigureAwait(false);

                await uow.CommitAsync().ConfigureAwait(false);
            }

            return Unit.Value;
        }

        private static void UpdateActorName(Domain.Model.Actor actor, UpdateActorCommand request)
        {
            actor.Name = new ActorName(request.ChangeActor.Name.Value);
        }

        private static void UpdateActorStatus(Domain.Model.Actor actor, UpdateActorCommand request)
        {
            actor.Status = Enum.Parse<ActorStatus>(request.ChangeActor.Status, true);
        }

        private async Task UpdateActorMarketRolesAndChildrenAsync(Domain.Model.Actor actor, UpdateActorCommand request)
        {
            actor.MarketRoles.Clear();

            foreach (var marketRole in MarketRoleMapper.Map(request.ChangeActor.MarketRoles))
            {
                actor.MarketRoles.Add(marketRole);
            }

            var allOrganizationActors = await _actorRepository
                .GetActorsAsync(actor.OrganizationId)
                .ConfigureAwait(false);

            var updatedActors = allOrganizationActors
                .Where(a => a.Id != actor.Id)
                .Append(actor)
                .ToList();

            _overlappingBusinessRolesRuleService.ValidateRolesAcrossActors(updatedActors);
            _allowedGridAreasRuleService.ValidateGridAreas(actor.MarketRoles);

            var allMarketRolesForActorGln = updatedActors
                .Where(x => x.ActorNumber == actor.ActorNumber)
                .SelectMany(x => x.MarketRoles)
                .Select(x => x.Function);

            _combinationOfBusinessRolesRuleService.ValidateCombinationOfBusinessRoles(allMarketRolesForActorGln);
        }
    }
}
