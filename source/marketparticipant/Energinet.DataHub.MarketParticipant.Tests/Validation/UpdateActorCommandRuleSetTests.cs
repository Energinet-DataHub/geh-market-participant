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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Actors;
using Energinet.DataHub.MarketParticipant.Application.Validation;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Validation;

[UnitTest]
public sealed class UpdateActorCommandRuleSetTests
{
    private const string ValidStatus = "Active";

    private static readonly Guid _validActorId = Guid.NewGuid();

    [Fact]
    public async Task Validate_ActorDto_ValidatesProperty()
    {
        // Arrange
        const string propertyName = nameof(UpdateActorCommand.ChangeActor);

        var target = new UpdateActorCommandRuleSet();
        var command = new UpdateActorCommand(_validActorId, null!);

        // Act
        var result = await target.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
    }

    [Fact]
    public async Task Validate_ActorId_ValidatesProperty()
    {
        // Arrange
        const string propertyName = nameof(UpdateActorCommand.ActorId);

        var validMeteringPointTypes = new[]
        {
            MeteringPointType.D05NetProduction.ToString()
        };
        var validGridAreas = new List<ActorGridAreaDto>
        {
            new(Guid.NewGuid(), validMeteringPointTypes)
        };
        var validMarketRole = new ActorMarketRoleDto(EicFunction.GridAccessProvider, validGridAreas, string.Empty);

        var actorDto = new ChangeActorDto(ValidStatus, new ActorNameDto("fake_name"), validMarketRole);

        var target = new UpdateActorCommandRuleSet();
        var command = new UpdateActorCommand(Guid.Empty, actorDto);

        // Act
        var result = await target.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
    }

    [Theory]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("  ", false)]
    [InlineData("Active", true)]
    [InlineData("Inactive", true)]
    [InlineData("Passive", true)]
    [InlineData("InvalidStatus", false)]
    public async Task Validate_ActorStatus_ValidatesProperty(string? value, bool isValid)
    {
        // Arrange
        const string propertyName = $"{nameof(UpdateActorCommand.ChangeActor)}.{nameof(ChangeActorDto.Status)}";

        var validMeteringPointTypes = new[]
        {
            MeteringPointType.D05NetProduction.ToString()
        };
        var validGridAreas = new List<ActorGridAreaDto>
        {
            new(Guid.NewGuid(), validMeteringPointTypes)
        };
        var validMarketRole = new ActorMarketRoleDto(EicFunction.BillingAgent, validGridAreas, string.Empty);

        var actorDto = new ChangeActorDto(value!, new ActorNameDto("fake_name"), validMarketRole);

        var target = new UpdateActorCommandRuleSet();
        var command = new UpdateActorCommand(_validActorId, actorDto);

        // Act
        var result = await target.ValidateAsync(command);

        // Assert
        if (isValid)
        {
            Assert.True(result.IsValid);
            Assert.DoesNotContain(propertyName, result.Errors.Select(x => x.PropertyName));
        }
        else
        {
            Assert.False(result.IsValid);
            Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
        }
    }

    [Fact]
    public async Task Validate_MarketRole_ValidatesProperty()
    {
        // Arrange
        const string propertyName = $"{nameof(UpdateActorCommand.ChangeActor)}.{nameof(ChangeActorDto.MarketRole)}";

        var actorDto = new ChangeActorDto(ValidStatus, new ActorNameDto("fake_name"), null!);

        var target = new UpdateActorCommandRuleSet();
        var command = new UpdateActorCommand(_validActorId, actorDto);

        // Act
        var result = await target.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
    }

    [Fact]
    public async Task Validate_NoMarketRoles_ValidatesProperty()
    {
        // Arrange
        const string propertyName = $"{nameof(UpdateActorCommand.ChangeActor)}.{nameof(ChangeActorDto.MarketRole)}";

        var actorDto = new ChangeActorDto(ValidStatus, new ActorNameDto("fake_name"), null!);

        var target = new UpdateActorCommandRuleSet();
        var command = new UpdateActorCommand(_validActorId, actorDto);

        // Act
        var result = await target.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
    }

    [Fact]
    public async Task Validate_NullMarketRole_ValidatesProperty()
    {
        // Arrange
        const string propertyName = $"{nameof(UpdateActorCommand.ChangeActor)}.{nameof(ChangeActorDto.MarketRole)}";

        var actorDto = new ChangeActorDto(ValidStatus, new ActorNameDto("fake_name"), null!);

        var target = new UpdateActorCommandRuleSet();
        var command = new UpdateActorCommand(_validActorId, actorDto);

        // Act
        var result = await target.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
    }

    [Theory]
    [InlineData(EicFunction.GridAccessProvider, true)]
    [InlineData(EicFunction.BalanceResponsibleParty, true)]
    [InlineData((EicFunction)65000, false)]
    public async Task Validate_MarketRoleFunction_ValidatesProperty(EicFunction value, bool isValid)
    {
        // Arrange
        const string propertyName = $"{nameof(UpdateActorCommand.ChangeActor)}.{nameof(ChangeActorDto.MarketRole)}.{nameof(ActorMarketRoleDto.EicFunction)}";

        var validMeteringPointTypes = new[]
        {
            MeteringPointType.D05NetProduction.ToString()
        };
        var validGridAreas = new List<ActorGridAreaDto>
        {
            new(Guid.NewGuid(), validMeteringPointTypes)
        };
        var validMarketRole = new ActorMarketRoleDto(value, validGridAreas, string.Empty);

        var actorDto = new ChangeActorDto(
            ValidStatus,
            new ActorNameDto("fake_name"),
            validMarketRole);

        var target = new UpdateActorCommandRuleSet();
        var command = new UpdateActorCommand(_validActorId, actorDto);

        // Act
        var result = await target.ValidateAsync(command);

        // Assert
        if (isValid)
        {
            Assert.True(result.IsValid);
            Assert.DoesNotContain(propertyName, result.Errors.Select(x => x.PropertyName));
        }
        else
        {
            Assert.False(result.IsValid);
            Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
        }
    }

    [Fact]
    public async Task Validate_MeteringPoints_ValidatesProperty()
    {
        // Arrange
        const string propertyName = $"{nameof(UpdateActorCommand.ChangeActor)}.{nameof(ChangeActorDto.MarketRole)}.GridAreas[0].MeteringPointTypes";

        var validGridAreas = new List<ActorGridAreaDto>
        {
            new(Guid.NewGuid(), null!)
        };
        var validMarketRole = new ActorMarketRoleDto(EicFunction.BillingAgent, validGridAreas, string.Empty);

        var actorDto = new ChangeActorDto(ValidStatus, new ActorNameDto("fake_name"), validMarketRole);

        var target = new UpdateActorCommandRuleSet();
        var command = new UpdateActorCommand(_validActorId, actorDto);

        // Act
        var result = await target.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
    }

    [Fact]
    public async Task Validate_NoMeteringPoints_IsAllowed()
    {
        // Arrange
        var validGridAreas = new List<ActorGridAreaDto>
        {
            new(Guid.NewGuid(), Array.Empty<string>())
        };
        var validMarketRole = new ActorMarketRoleDto(EicFunction.BillingAgent, validGridAreas, string.Empty);

        var actorDto = new ChangeActorDto(ValidStatus, new ActorNameDto("fake_name"), validMarketRole);

        var target = new UpdateActorCommandRuleSet();
        var command = new UpdateActorCommand(_validActorId, actorDto);

        // Act
        var result = await target.ValidateAsync(command);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_NullMeteringPoints_ValidatesProperty()
    {
        // Arrange
        const string propertyName = $"{nameof(UpdateActorCommand.ChangeActor)}.{nameof(ChangeActorDto.MarketRole)}.GridAreas[0].MeteringPointTypes[0]";

        var validGridAreas = new List<ActorGridAreaDto>
        {
            new(Guid.NewGuid(), [null!]),
        };
        var validMarketRole = new ActorMarketRoleDto(EicFunction.BillingAgent, validGridAreas, string.Empty);

        var actorDto = new ChangeActorDto(ValidStatus, new ActorNameDto("fake_name"), validMarketRole);

        var target = new UpdateActorCommandRuleSet();
        var command = new UpdateActorCommand(_validActorId, actorDto);

        // Act
        var result = await target.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
    }

    [Fact]
    public async Task Validate_MeteringPointType_ValidatesProperty()
    {
        // Arrange
        var propertyName = $"{nameof(UpdateActorCommand.ChangeActor)}.{nameof(ChangeActorDto.MarketRole)}.GridAreas[0].MeteringPointTypes";
        var validGridAreas = new List<ActorGridAreaDto>
        {
            new(Guid.NewGuid(), null!)
        };
        var validMarketRole = new ActorMarketRoleDto(EicFunction.BillingAgent, validGridAreas, string.Empty);

        var changeActorDto = new ChangeActorDto(ValidStatus, new ActorNameDto("fake_name"), validMarketRole);

        var target = new UpdateActorCommandRuleSet();
        var command = new UpdateActorCommand(_validActorId, changeActorDto);

        // Act
        var result = await target.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
    }

    [Theory]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("  ", false)]
    [InlineData("D06SupplyToGrid", true)]
    [InlineData("D07ConsumptionFromGrid", true)]
    [InlineData("D09OwnProduction", true)]
    [InlineData("D09OWNPRODUCTION", true)]
    [InlineData("D09OwnProductionXyz", false)]
    public async Task Validate_MeteringPointTypes_ValidatesProperty(string? value, bool isValid)
    {
        // Arrange
        var propertyName = $"{nameof(UpdateActorCommand.ChangeActor)}.{nameof(ChangeActorDto.MarketRole)}.GridAreas[0].MeteringPointTypes[0]";

        var validGridAreas = new List<ActorGridAreaDto>
        {
            new(Guid.NewGuid(), [value!]),
        };
        var validMarketRole = new ActorMarketRoleDto(EicFunction.BillingAgent, validGridAreas, string.Empty);

        var changeActorDto = new ChangeActorDto(ValidStatus, new ActorNameDto("fake_name"), validMarketRole);

        var target = new UpdateActorCommandRuleSet();
        var command = new UpdateActorCommand(_validActorId, changeActorDto);

        // Act
        var result = await target.ValidateAsync(command);

        // Assert
        if (isValid)
        {
            Assert.True(result.IsValid);
            Assert.DoesNotContain(propertyName, result.Errors.Select(x => x.PropertyName));
        }
        else
        {
            Assert.False(result.IsValid);
            Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
        }
    }
}
