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
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoleTemplates;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Repositories.Query;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.User;

public sealed class GetAvailableUserRoleTemplatesForActorCommandHandler
    : IRequestHandler<GetAvailableUserRoleTemplatesForActorCommand, GetUserRoleTemplatesResponse>
{
    private readonly IActorRepository _actorRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IUserRoleTemplateRepository _userRoleTemplateRepository;

    public GetAvailableUserRoleTemplatesForActorCommandHandler(
        IActorRepository actorRepository,
        IOrganizationRepository organizationRepository,
        IUserRoleTemplateRepository userRoleTemplateRepository)
    {
        _actorRepository = actorRepository;
        _organizationRepository = organizationRepository;
        _userRoleTemplateRepository = userRoleTemplateRepository;
    }

    public async Task<GetUserRoleTemplatesResponse> Handle(
        GetAvailableUserRoleTemplatesForActorCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var actor = await _actorRepository
            .GetActorAsync(request.ActorId)
            .ConfigureAwait(false);

        if (actor == null)
        {
            throw new NotFoundValidationException(request.ActorId);
        }

        var organization = await _organizationRepository
            .GetAsync(actor.OrganizationId)
            .ConfigureAwait(false);

        var eicFunctions = organization!
            .Actors
            .Where(a => a.Id == actor.ActorId)
            .SelectMany(a => a.MarketRoles)
            .Select(r => r.Function);

        var templates = await _userRoleTemplateRepository
            .GetAsync(eicFunctions)
            .ConfigureAwait(false);

        return new GetUserRoleTemplatesResponse(templates.Select(t =>
        {
            return new UserRoleTemplateDto(t.Id.Value, t.Name);
        }));
    }
}
