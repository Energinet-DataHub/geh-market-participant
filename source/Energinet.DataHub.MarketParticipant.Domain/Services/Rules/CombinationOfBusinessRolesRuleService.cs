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
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.BusinessRoles;

namespace Energinet.DataHub.MarketParticipant.Domain.Services.Rules
{
    public sealed class CombinationOfBusinessRolesRuleService : ICombinationOfBusinessRolesRuleService
    {
        public CombinationOfBusinessRolesRuleService(
            BalanceResponsiblePartyRole balanceResponsiblePartyRole,
            GridAccessProviderRole gridAccessProviderRole,
            BalancePowerSupplierRole balancePowerSupplierRole,
            ImbalanceSettlementResponsibleRole imbalanceSettlementResponsibleRole,
            MeteringPointAdministratorRole meteringPointAdministratorRole,
            MeteredDataAdministratorRole meteredDataAdministratorRole,
            SystemOperatorRole systemOperatorRole,
            MeteredDataResponsibleRole meteredDataResponsibleRole)
        {
            BalanceResponsiblePartyRole = balanceResponsiblePartyRole;
            GridAccessProviderRole = gridAccessProviderRole;
            BalancePowerSupplierRole = balancePowerSupplierRole;
            ImbalanceSettlementResponsibleRole = imbalanceSettlementResponsibleRole;
            MeteringPointAdministratorRole = meteringPointAdministratorRole;
            MeteredDataAdministratorRole = meteredDataAdministratorRole;
            SystemOperatorRole = systemOperatorRole;
            MeteredDataResponsibleRole = meteredDataResponsibleRole;

            DdkDdqMdr = BalanceResponsiblePartyRole.Functions
                .Concat(BalancePowerSupplierRole.Functions)
                .Concat(MeteredDataResponsibleRole.Functions)
                .ToList();

            DdmMdr = GridAccessProviderRole.Functions
                .Concat(MeteredDataResponsibleRole.Functions)
                .ToList();

            Ddz = MeteringPointAdministratorRole.Functions.ToList();
            Ddx = ImbalanceSettlementResponsibleRole.Functions.ToList();
            Dgl = MeteredDataAdministratorRole.Functions.ToList();
            Ez = SystemOperatorRole.Functions.ToList();
        }

        private MeteredDataResponsibleRole MeteredDataResponsibleRole { get; }
        private BalanceResponsiblePartyRole BalanceResponsiblePartyRole { get; }
        private GridAccessProviderRole GridAccessProviderRole { get; }
        private BalancePowerSupplierRole BalancePowerSupplierRole { get; }
        private ImbalanceSettlementResponsibleRole ImbalanceSettlementResponsibleRole { get; }
        private MeteringPointAdministratorRole MeteringPointAdministratorRole { get; }
        private MeteredDataAdministratorRole MeteredDataAdministratorRole { get; }
        private SystemOperatorRole SystemOperatorRole { get; }
        private List<EicFunction> DdkDdqMdr { get; }
        private List<EicFunction> DdmMdr { get; }
        private List<EicFunction> Ddz { get; }
        private List<EicFunction> Ddx { get; }
        private List<EicFunction> Dgl { get; }
        private List<EicFunction> Ez { get; }

        public void ValidateCombinationOfBusinessRoles(IList<MarketRole> marketRoles)
        {
            ArgumentNullException.ThrowIfNull(marketRoles);

            var marketRolesWithoutMdr = new List<MarketRole>();
            foreach (var marketRole in marketRoles)
            {
                if (marketRole.Function == EicFunction.MeteredDataResponsible)
                {
                }
            }

            List<bool> isContained = new();

            if (marketRoles.All(x => DdkDdqMdr.Contains(x.Function)))
            {
                isContained.Add(true);
            }

            if (marketRoles.All(x => DdmMdr.Contains(x.Function)))
            {
                isContained.Add(true);
            }

            if (marketRoles.All(x => Ddx.Contains(x.Function)))
            {
                isContained.Add(true);
            }

            if (marketRoles.All(x => Ddz.Contains(x.Function)))
            {
                isContained.Add(true);
            }

            if (marketRoles.All(x => Dgl.Contains(x.Function)))
            {
                isContained.Add(true);
            }

            if (marketRoles.All(x => Ez.Contains(x.Function)))
            {
                isContained.Add(true);
            }

            if (isContained.Count > 1)
            {
                throw new ValidationException(
                    "Cannot assign market roles, as the chosen combination of roles is not valid.");
            }
        }
    }
}
