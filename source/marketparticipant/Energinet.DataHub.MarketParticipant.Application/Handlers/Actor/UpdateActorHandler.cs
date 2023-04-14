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
using Energinet.DataHub.MarketParticipant.Application.Mappers;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services.Rules;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Actor
{
    public sealed class UpdateActorHandler : IRequestHandler<UpdateActorCommand>
    {
        private readonly IActorRepository _actorRepository;
        private readonly IUnitOfWorkProvider _unitOfWorkProvider;
        private readonly IOverlappingEicFunctionsRuleService _overlappingEicFunctionsRuleService;
        private readonly IAllowedGridAreasRuleService _allowedGridAreasRuleService;
        private readonly IExternalActorSynchronizationRepository _externalActorSynchronizationRepository;
        private readonly IUniqueMarketRoleGridAreaRuleService _uniqueMarketRoleGridAreaRuleRuleService;
        private readonly IActorStatusMarketRolesRuleService _actorStatusMarketRolesRuleService;

        public UpdateActorHandler(
            IActorRepository actorRepository,
            IUnitOfWorkProvider unitOfWorkProvider,
            IOverlappingEicFunctionsRuleService overlappingEicFunctionsRuleService,
            IAllowedGridAreasRuleService allowedGridAreasRuleService,
            IExternalActorSynchronizationRepository externalActorSynchronizationRepository,
            IUniqueMarketRoleGridAreaRuleService uniqueMarketRoleGridAreaRuleRuleService,
            IActorStatusMarketRolesRuleService actorStatusMarketRolesRuleService)
        {
            _actorRepository = actorRepository;
            _unitOfWorkProvider = unitOfWorkProvider;
            _overlappingEicFunctionsRuleService = overlappingEicFunctionsRuleService;
            _allowedGridAreasRuleService = allowedGridAreasRuleService;
            _externalActorSynchronizationRepository = externalActorSynchronizationRepository;
            _uniqueMarketRoleGridAreaRuleRuleService = uniqueMarketRoleGridAreaRuleRuleService;
            _actorStatusMarketRolesRuleService = actorStatusMarketRolesRuleService;
        }

        public async Task Handle(UpdateActorCommand request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));

            var actor = await _actorRepository
                .GetAsync(new ActorId(request.ActorId))
                .ConfigureAwait(false);

            if (actor == null)
            {
                throw new NotFoundValidationException(request.ActorId);
            }

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

                await uow.CommitAsync().ConfigureAwait(false);
            }
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

            _allowedGridAreasRuleService.ValidateGridAreas(actor.MarketRoles);
            _overlappingEicFunctionsRuleService.ValidateEicFunctionsAcrossActors(updatedActors);
        }
    }
}
