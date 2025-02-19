﻿// Copyright 2020 Energinet DataHub A/S
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
    public static IReadOnlyCollection<KnownPermission> All { get; } =
    [
        new(PermissionId.Dh2BridgeImport, "dh2-bridge:import", InstantPattern.ExtendedIso.Parse("2024-12-23T00:00:00Z").Value, [
            EicFunction.DataHubAdministrator
        ]),
        new(PermissionId.ActorsManage, "actors:manage", InstantPattern.ExtendedIso.Parse("2023-03-07T00:00:00Z").Value, [
            EicFunction.DataHubAdministrator
        ]),
        new(PermissionId.UsersManage, "users:manage", InstantPattern.ExtendedIso.Parse("2023-03-07T00:00:00Z").Value, [
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
            EicFunction.DataHubAdministrator,
            EicFunction.IndependentAggregator,
            EicFunction.SerialEnergyTrader,
            EicFunction.MeterOperator,
            EicFunction.ItSupplier
        ]),
        new(PermissionId.UsersView, "users:view", InstantPattern.ExtendedIso.Parse("2023-03-07T00:00:00Z").Value, [
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
            EicFunction.DataHubAdministrator,
            EicFunction.IndependentAggregator,
            EicFunction.SerialEnergyTrader,
            EicFunction.MeterOperator,
            EicFunction.ItSupplier
        ]),
        new(PermissionId.UserRolesManage, "user-roles:manage", InstantPattern.ExtendedIso.Parse("2023-03-07T00:00:00Z").Value, [
            EicFunction.DataHubAdministrator
        ]),
        new(PermissionId.ImbalancePricesManage, "imbalance-prices:manage", InstantPattern.ExtendedIso.Parse("2024-01-12T00:00:00Z").Value, [
            EicFunction.DataHubAdministrator
        ]),
        new(PermissionId.CalculationsManage, "calculations:manage", InstantPattern.ExtendedIso.Parse("2023-08-15T00:00:00Z").Value, [
            EicFunction.DataHubAdministrator
        ]),
        new(PermissionId.SettlementReportsManage, "settlement-reports:manage", InstantPattern.ExtendedIso.Parse("2023-08-15T00:00:00Z").Value, [
            EicFunction.EnergySupplier,
            EicFunction.GridAccessProvider,
            EicFunction.SystemOperator,
            EicFunction.DataHubAdministrator
        ]),
        new(PermissionId.ESettExchangeManage, "esett-exchange:manage", InstantPattern.ExtendedIso.Parse("2023-09-01T00:00:00Z").Value, [
            EicFunction.DataHubAdministrator
        ]),
        new(PermissionId.RequestAggregatedMeasureData, "request-aggregated-measured-data:view", InstantPattern.ExtendedIso.Parse("2023-10-04T00:00:00Z").Value, [
            EicFunction.GridAccessProvider,
            EicFunction.MeteredDataResponsible,
            EicFunction.EnergySupplier,
            EicFunction.BalanceResponsibleParty
        ]),
        new(PermissionId.ActorCredentialsManage, "actor-credentials:manage", InstantPattern.ExtendedIso.Parse("2023-10-27T00:00:00Z").Value, [
            EicFunction.DataHubAdministrator,
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
            EicFunction.IndependentAggregator,
            EicFunction.SerialEnergyTrader,
            EicFunction.MeterOperator,
            EicFunction.Delegated
        ]),
        new(PermissionId.ActorMasterDataManage, "actor-master-data:manage", InstantPattern.ExtendedIso.Parse("2024-02-08T00:00:00Z").Value, [
            EicFunction.BalanceResponsibleParty,
            EicFunction.BillingAgent,
            EicFunction.GridAccessProvider,
            EicFunction.ImbalanceSettlementResponsible,
            EicFunction.MeteredDataAdministrator,
            EicFunction.MeteredDataResponsible,
            EicFunction.MeteringPointAdministrator,
            EicFunction.SystemOperator,
            EicFunction.DanishEnergyAgency,
            EicFunction.DataHubAdministrator,
            EicFunction.IndependentAggregator,
            EicFunction.SerialEnergyTrader,
            EicFunction.MeterOperator,
            EicFunction.EnergySupplier,
            EicFunction.ItSupplier
        ]),
        new(PermissionId.DelegationManage, "delegation:manage", InstantPattern.ExtendedIso.Parse("2024-03-05T00:00:00Z").Value, [
            EicFunction.DataHubAdministrator
        ]),
        new(PermissionId.DelegationView, "delegation:view", InstantPattern.ExtendedIso.Parse("2024-03-05T00:00:00Z").Value, [
            EicFunction.BalanceResponsibleParty,
            EicFunction.GridAccessProvider,
            EicFunction.EnergySupplier,
            EicFunction.DataHubAdministrator
        ]),
        new(PermissionId.UsersReActivate, "users:reactivate", InstantPattern.ExtendedIso.Parse("2024-04-02T00:00:00Z").Value, [
            EicFunction.DataHubAdministrator
        ]),
        new(PermissionId.BalanceResponsibilityView, "balance-responsibility:view", InstantPattern.ExtendedIso.Parse("2024-04-15T00:00:00Z").Value, [
            EicFunction.DataHubAdministrator,
            EicFunction.EnergySupplier,
            EicFunction.BalanceResponsibleParty
        ]),
        new(PermissionId.RequestWholesaleSettlement, "request-wholesale-settlement:view", InstantPattern.ExtendedIso.Parse("2024-05-16T00:00:00Z").Value, [
            EicFunction.EnergySupplier,
            EicFunction.SystemOperator,
            EicFunction.GridAccessProvider
        ]),
        new(PermissionId.CalculationsView, "calculations:view", InstantPattern.ExtendedIso.Parse("2024-08-09T00:00:00Z").Value, [
            EicFunction.DataHubAdministrator
        ]),
        new(PermissionId.ImbalancePricesView, "imbalance-prices:view", InstantPattern.ExtendedIso.Parse("2024-08-13T00:00:00Z").Value, [
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
            EicFunction.DataHubAdministrator,
            EicFunction.IndependentAggregator,
            EicFunction.SerialEnergyTrader,
            EicFunction.MeterOperator,
            EicFunction.ItSupplier
        ]),
        new(PermissionId.MeteringPointSearch, "metering-point:search", InstantPattern.ExtendedIso.Parse("2024-11-29T00:00:00Z").Value, [
            EicFunction.DataHubAdministrator
        ]),
         new(PermissionId.MeteringPointEnergySupplierView, "metering-point-energy-supplier:view", InstantPattern.ExtendedIso.Parse("2025-02-18T00:00:00Z").Value, [
            EicFunction.DataHubAdministrator,
            EicFunction.EnergySupplier
        ]),
         new(PermissionId.CPRView, "cpr:view", InstantPattern.ExtendedIso.Parse("2025-02-18T00:00:00Z").Value, [
            EicFunction.DataHubAdministrator,
            EicFunction.EnergySupplier
        ])
    ];
}
