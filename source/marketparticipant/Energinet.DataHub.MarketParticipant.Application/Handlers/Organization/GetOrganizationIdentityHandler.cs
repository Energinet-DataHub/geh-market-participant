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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Organization;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Organization;

public sealed class GetOrganizationIdentityHandler : IRequestHandler<GetOrganizationIdentityCommand, GetOrganizationIdentityResponse>
{
    private readonly IOrganizationIdentityRepository _organizationIdentityRepository;

    public GetOrganizationIdentityHandler(IOrganizationIdentityRepository organizationIdentityRepository)
    {
        _organizationIdentityRepository = organizationIdentityRepository;
    }

    public async Task<GetOrganizationIdentityResponse> Handle(GetOrganizationIdentityCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var identity = await _organizationIdentityRepository.GetAsync(
            new BusinessRegisterIdentifier(request.BusinessRegisterIdentifier)).ConfigureAwait(false);

        return identity != null
            ? new GetOrganizationIdentityResponse(true, new OrganizationIdentityDto(identity.Name))
            : new GetOrganizationIdentityResponse(false, null);
    }
}
