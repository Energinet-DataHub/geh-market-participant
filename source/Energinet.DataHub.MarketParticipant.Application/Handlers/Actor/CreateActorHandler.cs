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
using Energinet.DataHub.MarketParticipant.Application.Commands.Actor;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Energinet.DataHub.MarketParticipant.Domain.Services.Rules;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Actor
{
    public sealed class CreateActorHandler : IRequestHandler<CreateActorCommand, CreateActorResponse>
    {
        private readonly IOrganizationExistsHelperService _organizationExistsHelperService;
        private readonly IActorFactoryService _actorFactoryService;
        private readonly ICombinationOfBusinessRolesRuleService _combinationOfBusinessRolesRuleService;

        public CreateActorHandler(
            IOrganizationExistsHelperService organizationExistsHelperService,
            IActorFactoryService actorFactoryService,
            ICombinationOfBusinessRolesRuleService combinationOfBusinessRolesRuleService)
        {
            _organizationExistsHelperService = organizationExistsHelperService;
            _actorFactoryService = actorFactoryService;
            _combinationOfBusinessRolesRuleService = combinationOfBusinessRolesRuleService;
        }

        public async Task<CreateActorResponse> Handle(CreateActorCommand request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));

            var organization = await _organizationExistsHelperService
                .EnsureOrganizationExistsAsync(request.OrganizationId)
                .ConfigureAwait(false);

            var actorGln = new ActorNumber(request.Actor.ActorNumber.Value);
            var marketRoles = CreateMarketRoles(request.Actor).ToList();

            _combinationOfBusinessRolesRuleService.ValidateCombinationOfBusinessRoles(marketRoles.Select(m => m.Function).ToList());

            var actor = await _actorFactoryService
                .CreateAsync(organization, actorGln, marketRoles)
                .ConfigureAwait(false);

            return new CreateActorResponse(actor.Id);
        }

        private static IEnumerable<ActorMarketRole> CreateMarketRoles(CreateActorDto actorDto)
        {
            foreach (var marketRole in actorDto.MarketRoles)
            {
                var function = Enum.Parse<EicFunction>(marketRole.EicFunction, true);
                yield return new ActorMarketRole(function, actorDto.GridAreas.Select(gridId => new ActorGridArea(gridId, actorDto.MeteringPointTypes.Select(e => MeteringPointType.FromName(e)))));
            }
        }
    }
}
