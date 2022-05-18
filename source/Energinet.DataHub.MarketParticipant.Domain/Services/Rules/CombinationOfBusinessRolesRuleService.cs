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
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.BusinessRoles;
using FluentValidation;

namespace Energinet.DataHub.MarketParticipant.Domain.Services.Rules
{
    public sealed class CombinationOfBusinessRolesRuleService : ICombinationOfBusinessRolesRuleService
    {
        public CombinationOfBusinessRolesRuleService()
        {
            BalanceResponsiblePartyRole = new BalanceResponsiblePartyRole();
            GridAccessProviderRole = new GridAccessProviderRole();
            BalancePowerSupplierRole = new BalancePowerSupplierRole();
            ImbalanceSettlementResponsibleRole = new ImbalanceSettlementResponsibleRole();
            MeteringPointAdministratorRole = new MeteringPointAdministratorRole();
            MeteredDataAdministratorRole = new MeteredDataAdministratorRole();
            SystemOperatorRole = new SystemOperatorRole();
            MeteredDataResponsibleRole = new MeteredDataResponsibleRole();

            var ddkDdqMdrHashSets = BalanceResponsiblePartyRole.Functions.ToHashSet();
            ddkDdqMdrHashSets.UnionWith(BalancePowerSupplierRole.Functions.Concat(MeteredDataResponsibleRole.Functions));
            DdkDdqMdr = new HashSet<EicFunction>(ddkDdqMdrHashSets);

            var ddmMdrHashSets = BalanceResponsiblePartyRole.Functions.ToHashSet();
            ddmMdrHashSets.UnionWith(BalancePowerSupplierRole.Functions.Concat(MeteredDataResponsibleRole.Functions));
            DdmMdr = new HashSet<EicFunction>(ddmMdrHashSets);

            Ddz = MeteringPointAdministratorRole.Functions.ToHashSet();
            Ddx = ImbalanceSettlementResponsibleRole.Functions.ToHashSet();
            Dgl = MeteredDataAdministratorRole.Functions.ToHashSet();
            Ez = SystemOperatorRole.Functions.ToHashSet();
            AllSets = new List<HashSet<EicFunction>> { DdkDdqMdr, DdmMdr, Ddz, Ddx, Dgl, Ez };
        }

        private MeteredDataResponsibleRole MeteredDataResponsibleRole { get; }
        private BalanceResponsiblePartyRole BalanceResponsiblePartyRole { get; }
        private GridAccessProviderRole GridAccessProviderRole { get; }
        private BalancePowerSupplierRole BalancePowerSupplierRole { get; }
        private ImbalanceSettlementResponsibleRole ImbalanceSettlementResponsibleRole { get; }
        private MeteringPointAdministratorRole MeteringPointAdministratorRole { get; }
        private MeteredDataAdministratorRole MeteredDataAdministratorRole { get; }
        private SystemOperatorRole SystemOperatorRole { get; }
        private HashSet<EicFunction> DdkDdqMdr { get; }
        private HashSet<EicFunction> DdmMdr { get; }
        private HashSet<EicFunction> Ddz { get; }
        private HashSet<EicFunction> Ddx { get; }
        private HashSet<EicFunction> Dgl { get; }
        private HashSet<EicFunction> Ez { get; }
        private List<HashSet<EicFunction>> AllSets { get; }

        public void ValidateCombinationOfBusinessRoles(IList<MarketRole> marketRoles)
        {
            ArgumentNullException.ThrowIfNull(marketRoles);
            var marketRolesHashSet = new HashSet<EicFunction>();

            foreach (var marketRole in marketRoles)
            {
                marketRolesHashSet.Add(marketRole.Function);
            }

            if (AllSets.All(knownSet => !marketRolesHashSet.IsSubsetOf(knownSet)))
            {
                throw new ValidationException(
                    "Cannot assign market roles, as the chosen combination of roles is not valid.");
            }
        }
    }
}
