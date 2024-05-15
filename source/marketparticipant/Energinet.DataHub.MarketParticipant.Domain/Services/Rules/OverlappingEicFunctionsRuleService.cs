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
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;

namespace Energinet.DataHub.MarketParticipant.Domain.Services.Rules;

public sealed class OverlappingEicFunctionsRuleService : IOverlappingEicFunctionsRuleService
{
    private readonly IActorRepository _actorRepository;

    public OverlappingEicFunctionsRuleService(IActorRepository actorRepository)
    {
        _actorRepository = actorRepository;
    }

    public async Task ValidateEicFunctionsAcrossActorsAsync(Actor actor)
    {
        ArgumentNullException.ThrowIfNull(actor);

        var actors = await _actorRepository
            .GetActorsAsync(actor.OrganizationId)
            .ConfigureAwait(false);

        var otherActorsInOrganization = actors
            .Where(a => a.Id != actor.Id)
            .ToList();

        ValidateUniquenessAcrossActors(actor, otherActorsInOrganization);
        ValidateAdministratorMarketRoleAcrossActors(actor, otherActorsInOrganization);
    }

    private static void ValidateUniquenessAcrossActors(Actor actor, IEnumerable<Actor> otherActorsInOrganization)
    {
        var allActors = otherActorsInOrganization
            .Append(actor)
            .GroupBy(x => x.ActorNumber);

        foreach (var actorsWithSameActorNumber in allActors)
        {
            var setOfSets = actorsWithSameActorNumber
                .Select(x => x.MarketRoles.Select(m => m.Function))
                .ToList();

            var usedMarketRoles = new HashSet<EicFunction>();

            foreach (var marketRole in setOfSets.SelectMany(x => x))
            {
                if (!usedMarketRoles.Add(marketRole))
                {
                    throw new ValidationException($"Cannot add '{marketRole}' as this market role is already assigned to another actor within the organization.")
                        .WithErrorCode("actor.market_role.reserved")
                        .WithArgs(("market_role", marketRole));
                }
            }
        }
    }

    private static void ValidateAdministratorMarketRoleAcrossActors(Actor actor, IEnumerable<Actor> otherActorsInOrganization)
    {
        if (actor.Status != ActorStatus.New ||
            actor.MarketRoles.All(a => a.Function != EicFunction.DataHubAdministrator))
        {
            return;
        }

        // DataHubAdministrator is only allowed if the organization already has DataHubAdministrator.
        if (otherActorsInOrganization.All(
                a => a.MarketRoles.All(
                    m => m.Function != EicFunction.DataHubAdministrator)))
        {
            throw new ValidationException($"Market role '{EicFunction.DataHubAdministrator}' cannot be used in this organization.")
                .WithErrorCode("actor.market_role.forbidden")
                .WithArgs(("market_role", EicFunction.DataHubAdministrator));
        }
    }
}
