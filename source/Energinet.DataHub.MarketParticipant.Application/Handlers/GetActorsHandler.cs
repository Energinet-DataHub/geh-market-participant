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

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Actor;
using Energinet.DataHub.MarketParticipant.Application.Mappers;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Utilities;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers
{
    public sealed class GetActorsHandler : IRequestHandler<GetActorsCommand, GetActorsResponse>
    {
        private readonly IOrganizationRepository _organizationRepository;

        public GetActorsHandler(IOrganizationRepository organizationRepository)
        {
            _organizationRepository = organizationRepository;
        }

        public async Task<GetActorsResponse> Handle(
            GetActorsCommand request,
            CancellationToken cancellationToken)
        {
            Guard.ThrowIfNull(request, nameof(request));

            var organization = await _organizationRepository
                .GetAsync(new OrganizationId(request.OrganizationId))
                .ConfigureAwait(false);

            if (organization == null)
            {
                throw new NotFoundValidationException(request.OrganizationId);
            }

            var actors = organization.Actors.Select(OrganizationMapper.Map);

            return new GetActorsResponse(actors);
        }
    }
}
