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
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using NodaTime.Text;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Model.Permissions;

/// <summary>
/// Contains all the registered permissions. See /docs/authorization.md for more information.
/// </summary>
public static class KnownPermissions
{
    public static IReadOnlyCollection<KnownPermission> All { get; } = new KnownPermission[]
    {
        new(PermissionId.OrganizationView, "organizations:view", InstantPattern.ExtendedIso.Parse("2023-03-07T00:00:00Z").Value, new[]
        {
            EicFunction.BalanceResponsibleParty,
            EicFunction.BillingAgent,
            EicFunction.EnergySupplier,
            EicFunction.GridAccessProvider,
            EicFunction.ImbalanceSettlementResponsible,
            EicFunction.MeteredDataAdministrator,
            EicFunction.MeteredDataResponsible,
            EicFunction.MeteringPointAdministrator,
            EicFunction.SystemOperator,
            EicFunction.DanishEnergyAgency,
            EicFunction.ElOverblik,
            EicFunction.DataHubAdministrator,
            EicFunction.IndependentAggregator,
            EicFunction.SerialEnergyTrader
        }),
        new(PermissionId.OrganizationManage, "organizations:manage", InstantPattern.ExtendedIso.Parse("2023-03-07T00:00:00Z").Value, new[]
        {
            EicFunction.DataHubAdministrator
        }),
        new(PermissionId.GridAreasManage, "grid-areas:manage", InstantPattern.ExtendedIso.Parse("2023-03-07T00:00:00Z").Value, new[]
        {
            EicFunction.DataHubAdministrator
        }),
        new(PermissionId.ActorManage, "actors:manage", InstantPattern.ExtendedIso.Parse("2023-03-07T00:00:00Z").Value, new[]
        {
            EicFunction.DataHubAdministrator
        }),
        new(PermissionId.UsersManage, "users:manage", InstantPattern.ExtendedIso.Parse("2023-03-07T00:00:00Z").Value, new[]
        {
            EicFunction.BalanceResponsibleParty,
            EicFunction.BillingAgent,
            EicFunction.EnergySupplier,
            EicFunction.GridAccessProvider,
            EicFunction.ImbalanceSettlementResponsible,
            EicFunction.MeteredDataAdministrator,
            EicFunction.MeteredDataResponsible,
            EicFunction.MeteringPointAdministrator,
            EicFunction.SystemOperator,
            EicFunction.DanishEnergyAgency,
            EicFunction.ElOverblik,
            EicFunction.DataHubAdministrator,
            EicFunction.IndependentAggregator,
            EicFunction.SerialEnergyTrader
        }),
        new(PermissionId.UsersView, "users:view", InstantPattern.ExtendedIso.Parse("2023-03-07T00:00:00Z").Value, new[]
        {
            EicFunction.BalanceResponsibleParty,
            EicFunction.BillingAgent,
            EicFunction.GridAccessProvider,
            EicFunction.ImbalanceSettlementResponsible,
            EicFunction.MeteredDataAdministrator,
            EicFunction.MeteredDataResponsible,
            EicFunction.MeteringPointAdministrator,
            EicFunction.SystemOperator,
            EicFunction.DanishEnergyAgency,
            EicFunction.ElOverblik,
            EicFunction.DataHubAdministrator,
            EicFunction.IndependentAggregator,
            EicFunction.SerialEnergyTrader
        }),
        new(PermissionId.UserRoleManage, "user-roles:manage", InstantPattern.ExtendedIso.Parse("2023-03-07T00:00:00Z").Value, new[]
        {
            EicFunction.DataHubAdministrator
        })
    };
}
