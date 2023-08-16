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
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Actor
{
    public sealed class CreateActorHandler : IRequestHandler<CreateActorCommand, CreateActorResponse>
    {
        private readonly IOrganizationExistsHelperService _organizationExistsHelperService;
        private readonly IActorFactoryService _actorFactoryService;

        public CreateActorHandler(
            IOrganizationExistsHelperService organizationExistsHelperService,
            IActorFactoryService actorFactoryService)
        {
            _organizationExistsHelperService = organizationExistsHelperService;
            _actorFactoryService = actorFactoryService;
        }

        public async Task<CreateActorResponse> Handle(CreateActorCommand request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));

            var organization = await _organizationExistsHelperService
                .EnsureOrganizationExistsAsync(request.Actor.OrganizationId)
                .ConfigureAwait(false);

            var actorNumber = ActorNumber.Create(request.Actor.ActorNumber.Value);
            var actorName = new ActorName(request.Actor.Name.Value);
            var marketRoles = MarketRoleMapper.Map(request.Actor.MarketRoles).ToList();

            var actor = await _actorFactoryService
                .CreateAsync(organization, actorNumber, actorName, marketRoles)
                .ConfigureAwait(false);

            return new CreateActorResponse(actor.Id.Value);
        }
    }
}
