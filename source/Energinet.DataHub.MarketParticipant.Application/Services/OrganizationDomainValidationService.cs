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
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Repositories.Query;

namespace Energinet.DataHub.MarketParticipant.Application.Services;

public class OrganizationDomainValidationService : IOrganizationDomainValidationService
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IActorQueryRepository _actorQueryRepository;

    public OrganizationDomainValidationService(
        IOrganizationRepository organizationRepository,
        IActorQueryRepository actorQueryRepository)
    {
        _organizationRepository = organizationRepository;
        _actorQueryRepository = actorQueryRepository;
    }

    public async Task ValidateUserEmailInsideOrganizationDomainsAsync(Guid actorId, string userInviteEmail)
    {
        ArgumentNullException.ThrowIfNull(userInviteEmail);

        var actor = await _actorQueryRepository
            .GetActorAsync(actorId)
            .ConfigureAwait(false);

        if (actor == null)
        {
            throw new NotFoundValidationException($"The specified actor {actorId} was not found.");
        }

        var organization = await _organizationRepository
            .GetAsync(actor.OrganizationId)
            .ConfigureAwait(false);

        if (organization == null)
        {
            throw new NotFoundValidationException($"The specified organization {actor.OrganizationId} was not found.");
        }

        if (!userInviteEmail.EndsWith("@" + organization.Domain.Value, StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException("User email not valid, should match organization domain");
        }
    }
}
