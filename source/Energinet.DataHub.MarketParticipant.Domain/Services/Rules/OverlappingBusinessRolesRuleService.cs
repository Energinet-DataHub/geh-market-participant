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

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Utilities;

namespace Energinet.DataHub.MarketParticipant.Domain.Services.Rules
{
    public sealed class OverlappingBusinessRolesRuleService : IOverlappingBusinessRolesRuleService
    {
        private readonly IBusinessRoleCodeDomainService _businessRoleCodeDomainService;

        public OverlappingBusinessRolesRuleService(IBusinessRoleCodeDomainService businessRoleCodeDomainService)
        {
            _businessRoleCodeDomainService = businessRoleCodeDomainService;
        }

        public void ValidateRolesAcrossActors(IEnumerable<Actor> actors)
        {
            ValidateRolesAcrossActors(actors, Enumerable.Empty<MarketRole>());
        }

        public void ValidateRolesAcrossActors(IEnumerable<Actor> actors, IEnumerable<MarketRole> newActorRoles)
        {
            Guard.ThrowIfNull(actors, nameof(actors));
            Guard.ThrowIfNull(newActorRoles, nameof(newActorRoles));

            var newBusinessRoles = CreateSet(newActorRoles);

            var allBusinessRoles = actors
                .SelectMany(actor => CreateSet(actor.MarketRoles))
                .Concat(newBusinessRoles);

            var usedRoles = new HashSet<BusinessRoleCode>();

            foreach (var businessRole in allBusinessRoles)
            {
                if (!usedRoles.Add(businessRole))
                {
                    throw new ValidationException($"Cannot add '{businessRole}' as this role is already assigned to another actor within the organization.");
                }
            }
        }

        private static IEnumerable<MarketRole> AreRolesUnique(IEnumerable<MarketRole> marketRoles)
        {
            var usedFunctions = new HashSet<EicFunction>();

            foreach (var marketRole in marketRoles)
            {
                if (usedFunctions.Add(marketRole.Function))
                {
                    yield return marketRole;
                }

                throw new ValidationException("The market roles cannot contain duplicates.");
            }
        }

        private IEnumerable<BusinessRoleCode> CreateSet(IEnumerable<MarketRole> marketRoles)
        {
            return _businessRoleCodeDomainService.GetBusinessRoleCodes(AreRolesUnique(marketRoles));
        }
    }
}
