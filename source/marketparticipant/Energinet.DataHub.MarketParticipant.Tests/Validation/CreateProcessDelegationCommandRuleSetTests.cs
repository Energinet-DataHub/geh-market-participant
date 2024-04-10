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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Delegations;
using Energinet.DataHub.MarketParticipant.Application.Validation;
using Energinet.DataHub.MarketParticipant.Domain.Model.Delegations;
using Xunit;
using Xunit.Categories;
namespace Energinet.DataHub.MarketParticipant.Tests.Validation;

[UnitTest]
public sealed class CreateProcessDelegationCommandRuleSetTests
{
    [Fact]
    public async Task Validate_CreateProcessDelegationDto_ValidatesProperty()
    {
        // Arrange
        const string propertyName = nameof(CreateProcessDelegationCommand.CreateDelegation);

        var target = new CreateProcessDelegationCommandRuleSet();
        var command = new CreateProcessDelegationCommand(null!);

        // Act
        var result = await target.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
    }

    [Theory]
    [InlineData("8F9B8218-BAE6-412B-B91B-0C78A55FF128", true)]
    [InlineData("00000000-0000-0000-0000-000000000000", false)]
    public async Task Validate_DelegatedFrom_ValidatesProperty(string value, bool isValid)
    {
        // Arrange
        const string propertyName = $"{nameof(CreateProcessDelegationCommand.CreateDelegation)}.{nameof(CreateProcessDelegationsDto.DelegatedFrom)}";
        var gridAreaList = new List<Guid>() { Guid.NewGuid() };
        var messageTypeList = new List<DelegatedProcess>() { DelegatedProcess.RequestEnergyResults };
        var delegationDto = new CreateProcessDelegationsDto(
            Guid.Parse(value),
            Guid.NewGuid(),
            gridAreaList,
            messageTypeList,
            DateTimeOffset.UtcNow);

        var target = new CreateProcessDelegationCommandRuleSet();
        var command = new CreateProcessDelegationCommand(delegationDto);

        // Act
        var result = await target.ValidateAsync(command);

        // Assert
        if (isValid)
        {
            Assert.DoesNotContain(propertyName, result.Errors.Select(x => x.PropertyName));
            Assert.True(result.IsValid);
        }
        else
        {
            Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
            Assert.False(result.IsValid);
        }
    }

    [Theory]
    [InlineData("8F9B8218-BAE6-412B-B91B-0C78A55FF128", true)]
    [InlineData("00000000-0000-0000-0000-000000000000", false)]
    public async Task Validate_DelegatedTo_ValidatesProperty(string value, bool isValid)
    {
        // Arrange
        const string propertyName = $"{nameof(CreateProcessDelegationCommand.CreateDelegation)}.{nameof(CreateProcessDelegationsDto.DelegatedTo)}";
        var gridAreaList = new List<Guid>() { Guid.NewGuid() };
        var messageTypeList = new List<DelegatedProcess>() { DelegatedProcess.RequestEnergyResults };
        var delegationDto = new CreateProcessDelegationsDto(
            Guid.NewGuid(),
            Guid.Parse(value),
            gridAreaList,
            messageTypeList,
            DateTimeOffset.UtcNow);

        var target = new CreateProcessDelegationCommandRuleSet();
        var command = new CreateProcessDelegationCommand(delegationDto);

        // Act
        var result = await target.ValidateAsync(command);

        // Assert
        if (isValid)
        {
            Assert.DoesNotContain(propertyName, result.Errors.Select(x => x.PropertyName));
            Assert.True(result.IsValid);
        }
        else
        {
            Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
            Assert.False(result.IsValid);
        }
    }

    [Theory]
    [InlineData(new[] { "8F9B8218-BAE6-412B-B91B-0C78A55FF128", "8F9B8218-BAE6-412B-B91B-0C78A55FF122" }, true)]
    [InlineData(new[] { "00000000-0000-0000-0000-000000000000", "8F9B8218-BAE6-412B-B91B-0C78A55FF128" }, false)]
    [InlineData(new[] { "8F9B8218-BAE6-412B-B91B-0C78A55FF128", "00000000-0000-0000-0000-000000000000" }, false)]
    [InlineData(new string[] { }, false)]
    public async Task Validate_GridAreas_ValidatesProperty(string[] value, bool isValid)
    {
        // Arrange
        const string propertyName = $"{nameof(CreateProcessDelegationCommand.CreateDelegation)}.{nameof(CreateProcessDelegationsDto.GridAreas)}";
        var gridAreaList = value.Select(Guid.Parse).ToList();
        var messageTypeList = new List<DelegatedProcess>() { DelegatedProcess.RequestEnergyResults };
        var delegationDto = new CreateProcessDelegationsDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            gridAreaList,
            messageTypeList,
            DateTimeOffset.UtcNow);

        var target = new CreateProcessDelegationCommandRuleSet();
        var command = new CreateProcessDelegationCommand(delegationDto);

        // Act
        var result = await target.ValidateAsync(command);

        // Assert
        if (isValid)
        {
            Assert.DoesNotContain(result.Errors, x => x.PropertyName.StartsWith(propertyName, StringComparison.OrdinalIgnoreCase));
            Assert.True(result.IsValid);
        }
        else
        {
            Assert.Contains(result.Errors, x => x.PropertyName.StartsWith(propertyName, StringComparison.OrdinalIgnoreCase));
            Assert.False(result.IsValid);
        }
    }

    [Theory]
    [InlineData(new[] { DelegatedProcess.ReceiveEnergyResults, DelegatedProcess.ReceiveWholesaleResults }, true)]
    [InlineData(new[] { DelegatedProcess.RequestEnergyResults, (DelegatedProcess)550 }, false)]
    [InlineData(new DelegatedProcess[] { }, false)]
    public async Task Validate_Process_ValidatesProperty(DelegatedProcess[] value, bool isValid)
    {
        // Arrange
        const string propertyName = $"{nameof(CreateProcessDelegationCommand.CreateDelegation)}.{nameof(CreateProcessDelegationsDto.DelegatedProcesses)}";
        var gridAreaList = new List<Guid>() { Guid.NewGuid() };
        var messageTypeList = new List<DelegatedProcess>(value);
        var delegationDto = new CreateProcessDelegationsDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            gridAreaList,
            messageTypeList,
            DateTimeOffset.UtcNow);

        var target = new CreateProcessDelegationCommandRuleSet();
        var command = new CreateProcessDelegationCommand(delegationDto);

        // Act
        var result = await target.ValidateAsync(command);

        // Assert
        if (isValid)
        {
            Assert.DoesNotContain(result.Errors, x => x.PropertyName.StartsWith(propertyName, StringComparison.OrdinalIgnoreCase));
            Assert.True(result.IsValid);
        }
        else
        {
            Assert.Contains(result.Errors, x => x.PropertyName.StartsWith(propertyName, StringComparison.OrdinalIgnoreCase));
            Assert.False(result.IsValid);
        }
    }

    [Fact]
    public async Task Validate_StartsAt_ValidatesProperty()
    {
        var dateTimeOffset = DateTimeOffset.UtcNow.Date;
        var cases = new[]
        {
            new { Time = dateTimeOffset, IsValid = true },
            new { Time = dateTimeOffset.AddDays(5), IsValid = true },
            new { Time = dateTimeOffset.AddDays(-1), IsValid = false },
        };

        foreach (var testCase in cases)
        {
            // Arrange
            const string propertyName = $"{nameof(CreateProcessDelegationCommand.CreateDelegation)}.{nameof(CreateProcessDelegationsDto.StartsAt)}";
            var gridAreaList = new List<Guid> { Guid.NewGuid() };
            var messageTypeList = new List<DelegatedProcess> { DelegatedProcess.RequestEnergyResults };
            var delegationDto = new CreateProcessDelegationsDto(
                Guid.NewGuid(),
                Guid.NewGuid(),
                gridAreaList,
                messageTypeList,
                testCase.Time);

            var target = new CreateProcessDelegationCommandRuleSet();
            var command = new CreateProcessDelegationCommand(delegationDto);

            // Act
            var result = await target.ValidateAsync(command);

            // Assert
            if (testCase.IsValid)
            {
                Assert.DoesNotContain(result.Errors, x => x.PropertyName.StartsWith(propertyName, StringComparison.OrdinalIgnoreCase));
                Assert.True(result.IsValid);
            }
            else
            {
                Assert.Contains(result.Errors, x => x.PropertyName.StartsWith(propertyName, StringComparison.OrdinalIgnoreCase));
                Assert.False(result.IsValid);
            }
        }
    }
}
