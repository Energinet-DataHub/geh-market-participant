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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Actor;
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
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IOrganizationExistsHelperService _organizationExistsHelperService;
        private readonly IUnitOfWorkProvider _unitOfWorkProvider;
        private readonly IActorIntegrationEventsQueueService _actorIntegrationEventsQueueService;
        private readonly IOverlappingBusinessRolesRuleService _overlappingBusinessRolesRuleService;
        private readonly IAllowedGridAreasRuleService _allowedGridAreasRuleService;

        public UpdateActorHandler(
            IOrganizationRepository organizationRepository,
            IOrganizationExistsHelperService organizationExistsHelperService,
            IUnitOfWorkProvider unitOfWorkProvider,
            IActorIntegrationEventsQueueService actorIntegrationEventsQueueService,
            IOverlappingBusinessRolesRuleService overlappingBusinessRolesRuleService,
            IAllowedGridAreasRuleService allowedGridAreasRuleService)
        {
            _organizationRepository = organizationRepository;
            _organizationExistsHelperService = organizationExistsHelperService;
            _unitOfWorkProvider = unitOfWorkProvider;
            _actorIntegrationEventsQueueService = actorIntegrationEventsQueueService;
            _overlappingBusinessRolesRuleService = overlappingBusinessRolesRuleService;
            _allowedGridAreasRuleService = allowedGridAreasRuleService;
        }

        [SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "Issue: https://github.com/dotnet/roslyn-analyzers/issues/5712")]
        public async Task<Unit> Handle(UpdateActorCommand request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));

            var organization = await _organizationExistsHelperService
                .EnsureOrganizationExistsAsync(request.OrganizationId)
                .ConfigureAwait(false);

            var actorId = request.ActorId;
            var actor = organization
                .Actors
                .SingleOrDefault(actor => actor.Id == actorId);

            if (actor == null)
            {
                throw new NotFoundValidationException(actorId);
            }

            UpdateActorStatus(actor, request);
            UpdateActorMarketRoles(organization, actor, request);
            UpdateActorGridAreas(actor, request);
            UpdateActorMeteringPointTypes(actor, request);

            await using var uow = await _unitOfWorkProvider
                .NewUnitOfWorkAsync()
                .ConfigureAwait(false);

            await _organizationRepository
                .AddOrUpdateAsync(organization)
                .ConfigureAwait(false);

            await _actorIntegrationEventsQueueService
                .EnqueueActorUpdatedEventAsync(organization.Id, actor)
                .ConfigureAwait(false);

            await uow.CommitAsync().ConfigureAwait(false);

            return Unit.Value;
        }

        private static void UpdateActorStatus(Domain.Model.Actor actor, UpdateActorCommand request)
        {
            actor.Status = Enum.Parse<ActorStatus>(request.ChangeActor.Status, true);
        }

        private static void UpdateActorMeteringPointTypes(Domain.Model.Actor actor, UpdateActorCommand request)
        {
            actor.MeteringPointTypes.Clear();

            var meteringPointTypes = request.ChangeActor.MeteringPointTypes.Select(t => MeteringPointType.FromName(t, true));
            var meteringPointTypesToAdd = meteringPointTypes.DistinctBy(type => type.Value);

            foreach (var meteringPointType in meteringPointTypesToAdd)
            {
                actor.MeteringPointTypes.Add(meteringPointType);
            }
        }

        private void UpdateActorMarketRoles(Domain.Model.Organization organization, Domain.Model.Actor actor, UpdateActorCommand request)
        {
            actor.MarketRoles.Clear();

            foreach (var marketRoleDto in request.ChangeActor.MarketRoles)
            {
                var function = Enum.Parse<EicFunction>(marketRoleDto.EicFunction, true);
                actor.MarketRoles.Add(new MarketRole(function));
            }

            _overlappingBusinessRolesRuleService.ValidateRolesAcrossActors(organization.Actors);
        }

        private void UpdateActorGridAreas(Domain.Model.Actor actor, UpdateActorCommand request)
        {
            actor.GridAreas.Clear();

            foreach (var gridAreaId in request.ChangeActor.GridAreas ?? Array.Empty<Guid>())
            {
                actor.GridAreas.Add(new GridAreaId(gridAreaId));
            }

            _allowedGridAreasRuleService.ValidateGridAreas(actor.GridAreas, actor.MarketRoles);
        }
    }
}
