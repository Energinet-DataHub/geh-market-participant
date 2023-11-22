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
using Energinet.DataHub.MarketParticipant.Application.Commands.Actor;
using Energinet.DataHub.MarketParticipant.Application.Validation;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Validation
{
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

        [Theory]
        [InlineData("", false)]
        [InlineData(null, false)]
        [InlineData("  ", false)]
        [InlineData("Active", true)]
        [InlineData("Inactive", true)]
        [InlineData("Passive", true)]
        [InlineData("InvalidStatus", false)]
        public async Task Validate_ActorStatus_ValidatesProperty(string value, bool isValid)
        {
            // Arrange
            const string propertyName = $"{nameof(UpdateActorCommand.ChangeActor)}.{nameof(ChangeActorDto.Status)}";

            var validMeteringPointTypes = new[] { MeteringPointType.D05NetProduction.ToString() };
            var validGridAreas = new List<ActorGridAreaDto> { new(Guid.NewGuid(), validMeteringPointTypes) };
            var validMarketRoles = new List<ActorMarketRoleDto> { new(EicFunction.BillingAgent, validGridAreas, string.Empty) };

            var actorDto = new ChangeActorDto(value, new ActorNameDto("fake_name"), validMarketRoles);

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
            const string propertyName = $"{nameof(UpdateActorCommand.ChangeActor)}.{nameof(ChangeActorDto.MarketRoles)}";

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
            const string propertyName = $"{nameof(UpdateActorCommand.ChangeActor)}.{nameof(ChangeActorDto.MarketRoles)}";

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
            const string propertyName = $"{nameof(UpdateActorCommand.ChangeActor)}.{nameof(ChangeActorDto.MarketRoles)}[0]";

            var actorDto = new ChangeActorDto(ValidStatus, new ActorNameDto("fake_name"), new ActorMarketRoleDto[] { null! });

            var target = new UpdateActorCommandRuleSet();
            var command = new UpdateActorCommand(_validActorId, actorDto);

            // Act
            var result = await target.ValidateAsync(command);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
        }

        [Theory]
        [InlineData(EicFunction.BalanceResponsibleParty, true)]
        [InlineData(EicFunction.GridAccessProvider, true)]
        public async Task Validate_MarketRoleFunction_ValidatesProperty(EicFunction value, bool isValid)
        {
            // Arrange
            const string propertyName = $"{nameof(UpdateActorCommand.ChangeActor)}.{nameof(ChangeActorDto.MarketRoles)}[0].{nameof(ActorMarketRoleDto.EicFunction)}";

            var validMeteringPointTypes = new[] { MeteringPointType.D05NetProduction.ToString() };
            var validGridAreas = new List<ActorGridAreaDto> { new(Guid.NewGuid(), validMeteringPointTypes) };
            var validMarketRoles = new List<ActorMarketRoleDto> { new(value, validGridAreas, string.Empty) };

            var actorDto = new ChangeActorDto(
                ValidStatus,
                new ActorNameDto("fake_name"),
                validMarketRoles);

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
        public async Task Validate_DuplicateMarketRoleFunctions_ValidatesProperty()
        {
            // Arrange
            const string propertyName = $"{nameof(UpdateActorCommand.ChangeActor)}.{nameof(ChangeActorDto.MarketRoles)}";

            var validMeteringPointTypes = new[] { MeteringPointType.D05NetProduction.ToString() };
            var validGridAreas = new List<ActorGridAreaDto> { new(Guid.NewGuid(), validMeteringPointTypes) };
            var validMarketRoles = new List<ActorMarketRoleDto>
            {
                new(EicFunction.GridAccessProvider, validGridAreas, string.Empty),
                new(EicFunction.GridAccessProvider, validGridAreas, string.Empty),
            };

            var actorDto = new ChangeActorDto(
                ValidStatus,
                new ActorNameDto("fake_name"),
                validMarketRoles);

            var target = new UpdateActorCommandRuleSet();
            var command = new UpdateActorCommand(_validActorId, actorDto);

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
        public async Task Validate_MeteringPointTypes_ValidatesProperty(string value, bool isValid)
        {
            // Arrange
            var propertyName = $"{nameof(UpdateActorCommand.ChangeActor)}.{nameof(ChangeActorDto.MarketRoles)}[0].GridAreas[0].MeteringPointTypes[0]";

            var validGridAreas = new List<ActorGridAreaDto> { new(Guid.NewGuid(), new[] { value }) };
            var validMarketRoles = new List<ActorMarketRoleDto> { new(EicFunction.BillingAgent, validGridAreas, string.Empty) };

            var changeActorDto = new ChangeActorDto(ValidStatus, new ActorNameDto("fake_name"), validMarketRoles);

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
}
