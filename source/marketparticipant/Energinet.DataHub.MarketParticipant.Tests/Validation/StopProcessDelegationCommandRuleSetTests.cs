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
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Delegations;
using Energinet.DataHub.MarketParticipant.Application.Validation;
using Xunit;
using Xunit.Categories;
namespace Energinet.DataHub.MarketParticipant.Tests.Validation;

[UnitTest]
public sealed class StopProcessDelegationCommandRuleSetTests
{
    [Fact]
    public async Task Validate_DelegationId_ValidatesProperty()
    {
        // Arrange
        const string propertyName = nameof(StopProcessDelegationCommand.DelegationId);

        var target = new StopProcessDelegationCommandRuleSet();
        var command = new StopProcessDelegationCommand(Guid.Empty, new StopProcessDelegationDto(Guid.NewGuid(), DateTimeOffset.UtcNow.Date.AddDays(5)));

        // Act
        var result = await target.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
    }

    [Fact]
    public async Task Validate_StopProcessDelegationDto_ValidatesProperty()
    {
        // Arrange
        const string propertyName = nameof(StopProcessDelegationCommand.StopProcessDelegation);

        var target = new StopProcessDelegationCommandRuleSet();
        var command = new StopProcessDelegationCommand(Guid.NewGuid(), null!);

        // Act
        var result = await target.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
    }

    [Fact]
    public async Task Validate_StopsAt_ValidatesProperty()
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
            const string propertyName = $"{nameof(StopProcessDelegationCommand.StopProcessDelegation)}.{nameof(StopProcessDelegationDto.StopsAt)}";
            var delegationDto = new StopProcessDelegationDto(
                Guid.NewGuid(),
                testCase.Time);

            var target = new StopProcessDelegationCommandRuleSet();
            var command = new StopProcessDelegationCommand(Guid.NewGuid(), delegationDto);

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
