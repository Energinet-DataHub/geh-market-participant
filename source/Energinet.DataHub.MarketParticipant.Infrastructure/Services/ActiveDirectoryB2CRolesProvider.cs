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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Infrastructure.Model.ActiveDirectory;
using Microsoft.Graph;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Services
{
    public class ActiveDirectoryB2CRolesProvider : IActiveDirectoryB2CRolesProvider
    {
        private readonly GraphServiceClient _graphClient;
        private readonly string _appObjectId;
        private readonly ActiveDirectoryB2CRoles _activeDirectoryB2CRoles;

        public ActiveDirectoryB2CRolesProvider(
            GraphServiceClient graphClient,
            string appObjectId)
        {
            _graphClient = graphClient;
            _appObjectId = appObjectId;
            _activeDirectoryB2CRoles = new ActiveDirectoryB2CRoles();
        }

        public async Task<ActiveDirectoryB2CRoles> GetB2CRolesAsync()
        {
            if (_activeDirectoryB2CRoles.IsLoaded)
            {
                return _activeDirectoryB2CRoles;
            }

            var application = await _graphClient.Applications[_appObjectId]
                .Request()
                .Select(a => new { a.DisplayName, a.AppRoles })
                .GetAsync()
                .ConfigureAwait(false);

            if (application is null)
            {
                throw new InvalidOperationException(
                    $"No application, '{nameof(application)}', was found in Active Directory.");
            }

            foreach (var appRole in application.AppRoles)
            {
                switch (appRole.Value)
                {
                    case "balanceresponsibleparty":
                        _activeDirectoryB2CRoles.EicRolesMapped.Add(EicFunction.BalanceResponsibleParty, appRole.Id!.Value);
                        break;
                    case "gridaccessprovider":
                        _activeDirectoryB2CRoles.EicRolesMapped.Add(EicFunction.GridAccessProvider, appRole.Id!.Value);
                        break;
                    case "billingagent":
                        _activeDirectoryB2CRoles.EicRolesMapped.Add(EicFunction.BillingAgent, appRole.Id!.Value);
                        break;
                    case "eloverblik":
                        _activeDirectoryB2CRoles.EicRolesMapped.Add(EicFunction.ElOverblik, appRole.Id!.Value);
                        break;
                    case "energysupplier":
                        _activeDirectoryB2CRoles.EicRolesMapped.Add(EicFunction.EnergySupplier, appRole.Id!.Value);
                        break;
                    case "independentaggregator":
                        _activeDirectoryB2CRoles.EicRolesMapped.Add(EicFunction.IndependentAggregator, appRole.Id!.Value);
                        break;
                    case "systemoperator":
                        _activeDirectoryB2CRoles.EicRolesMapped.Add(EicFunction.SystemOperator, appRole.Id!.Value);
                        break;
                    case "danishenergyagency":
                        _activeDirectoryB2CRoles.EicRolesMapped.Add(EicFunction.DanishEnergyAgency, appRole.Id!.Value);
                        break;
                    case "dataHubadministrator":
                        _activeDirectoryB2CRoles.EicRolesMapped.Add(EicFunction.DataHubAdministrator, appRole.Id!.Value);
                        break;
                    case "imbalancesettlementresponsible":
                        _activeDirectoryB2CRoles.EicRolesMapped.Add(EicFunction.ImbalanceSettlementResponsible, appRole.Id!.Value);
                        break;
                    case "metereddataadministrator":
                        _activeDirectoryB2CRoles.EicRolesMapped.Add(EicFunction.MeteredDataAdministrator, appRole.Id!.Value);
                        break;
                    case "metereddataresponsible":
                        _activeDirectoryB2CRoles.EicRolesMapped.Add(EicFunction.MeteredDataResponsible, appRole.Id!.Value);
                        break;
                    case "meteringpointadministrator":
                        _activeDirectoryB2CRoles.EicRolesMapped.Add(EicFunction.MeteringPointAdministrator, appRole.Id!.Value);
                        break;
                    case "serialenergytrader":
                        _activeDirectoryB2CRoles.EicRolesMapped.Add(EicFunction.SerialEnergyTrader, appRole.Id!.Value);
                        break;
                }
            }

            // Verify that all EIC functions has a corresponding app role
            return _activeDirectoryB2CRoles;
        }
    }
}
