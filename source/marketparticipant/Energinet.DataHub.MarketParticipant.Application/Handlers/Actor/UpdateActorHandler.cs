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
using Energinet.DataHub.MarketParticipant.Application.Extensions;
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
        private readonly IExternalActorSynchronizationRepository _externalActorSynchronizationRepository;
        private readonly IUniqueMarketRoleGridAreaRuleService _uniqueMarketRoleGridAreaRuleService;
        private readonly IDomainEventRepository _domainEventRepository;

        public UpdateActorHandler(
            IActorRepository actorRepository,
            IUnitOfWorkProvider unitOfWorkProvider,
            IOverlappingEicFunctionsRuleService overlappingEicFunctionsRuleService,
            IExternalActorSynchronizationRepository externalActorSynchronizationRepository,
            IUniqueMarketRoleGridAreaRuleService uniqueMarketRoleGridAreaRuleService,
            IDomainEventRepository domainEventRepository)
        {
            _actorRepository = actorRepository;
            _unitOfWorkProvider = unitOfWorkProvider;
            _overlappingEicFunctionsRuleService = overlappingEicFunctionsRuleService;
            _externalActorSynchronizationRepository = externalActorSynchronizationRepository;
            _uniqueMarketRoleGridAreaRuleService = uniqueMarketRoleGridAreaRuleService;
            _domainEventRepository = domainEventRepository;
        }

        public async Task<Unit> Handle(UpdateActorCommand request, CancellationToken cancellationToken)
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

                await _externalActorSynchronizationRepository
                    .ScheduleAsync(actor.Id.Value)
                    .ConfigureAwait(false);

                await uow.CommitAsync().ConfigureAwait(false);
            }

            return Unit.Value;
        }

        private static void UpdateAggregate(Domain.Model.Actor actor, ChangeActorDto changes)
        {
            var incomingMarketRoles = MarketRoleMapper.Map(changes.MarketRoles);
            var existingMarketRoles = actor.MarketRoles;

            var joinedMarketRoles = EnumerableExtensions
                .FullOuterJoin(
                    incomingMarketRoles,
                    existingMarketRoles,
                    (incomingRole, existingRole) => incomingRole.Function == existingRole.Function);

            foreach (var (incomingRole, existingRole) in joinedMarketRoles)
            {
                if (existingRole is not null)
                {
                    actor.RemoveMarketRole(existingRole);
                }

                if (incomingRole is not null)
                {
                    actor.AddMarketRole(incomingRole);
                }
            }

            actor.Name = new ActorName(changes.Name.Value);
            actor.Status = Enum.Parse<ActorStatus>(changes.Status, true);
        }

        private async Task ValidateAggregateAsync(Domain.Model.Actor actor)
        {
            await _uniqueMarketRoleGridAreaRuleService.ValidateAsync(actor).ConfigureAwait(false);

            var allOrganizationActors = await _actorRepository
                .GetActorsAsync(actor.OrganizationId)
                .ConfigureAwait(false);

            var updatedActors = allOrganizationActors
                .Where(a => a.Id != actor.Id)
                .Append(actor)
                .ToList();

            _overlappingEicFunctionsRuleService.ValidateEicFunctionsAcrossActors(updatedActors);
        }
    }
}
