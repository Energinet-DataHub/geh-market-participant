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
using Energinet.DataHub.MarketParticipant.Domain.Services.Rules;
using FluentValidation;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Services;

[UnitTest]
public class CombinationOfBusinessRolesRuleServiceTests
{
    [Fact]
    public void ValidateCombinationOfBusinessRoles_NullMarketRolesArgument_ThrowsException()
    {
        // Arrange
        var target = new CombinationOfBusinessRolesRuleService(
            new BalanceResponsiblePartyRole(),
            new GridAccessProviderRole(),
            new BalancePowerSupplierRole(),
            new ImbalanceSettlementResponsibleRole(),
            new MeteringPointAdministratorRole(),
            new MeteredDataAdministratorRole(),
            new SystemOperatorRole(),
            new MeteredDataResponsibleRole());

        // Act + Assert
        Assert.Throws<ArgumentNullException>(() => target.ValidateCombinationOfBusinessRoles(null!));
    }

    [Fact]
    public void ValidateCombinationOfBusinessRoles_InvalidCombinationOfRoles_ThrowsException()
    {
        // Arrange
        var target = new CombinationOfBusinessRolesRuleService(
            new BalanceResponsiblePartyRole(),
            new GridAccessProviderRole(),
            new BalancePowerSupplierRole(),
            new ImbalanceSettlementResponsibleRole(),
            new MeteringPointAdministratorRole(),
            new MeteredDataAdministratorRole(),
            new SystemOperatorRole(),
            new MeteredDataResponsibleRole());

        var ez = new SystemOperatorRole();
        var dgl = new MeteredDataAdministratorRole();

        var invalidCombinationOfBusinessRoles = new List<MarketRole>() { new(ez.Functions.First()), new(dgl.Functions.First()) };

        // Act + Assert
        Assert.Throws<ValidationException>(() => target.ValidateCombinationOfBusinessRoles(invalidCombinationOfBusinessRoles));
    }
}
