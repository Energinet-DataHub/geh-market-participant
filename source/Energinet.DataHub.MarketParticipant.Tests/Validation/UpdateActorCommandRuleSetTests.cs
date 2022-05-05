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

        private static readonly Guid _validOrganizationId = Guid.NewGuid();
        private static readonly Guid _validActorId = Guid.NewGuid();

        private static readonly MarketRoleDto[] _validMarketRoles = { new("CapacityTrader") };
        private static readonly string[] _validMeteringPointTypes = { MeteringPointType.D05NetProduction.Name };

        [Fact]
        public async Task Validate_ActorDto_ValidatesProperty()
        {
            // Arrange
            const string propertyName = nameof(UpdateActorCommand.ChangeActor);

            var target = new UpdateActorCommandRuleSet();
            var command = new UpdateActorCommand(_validOrganizationId, _validActorId, null!);

            // Act
            var result = await target.ValidateAsync(command).ConfigureAwait(false);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
        }

        [Fact]
        public async Task Validate_OrganizationId_ValidatesProperty()
        {
            // Arrange
            const string propertyName = nameof(UpdateActorCommand.OrganizationId);

            var actorDto = new ChangeActorDto(ValidStatus, Array.Empty<MarketRoleDto>(), Array.Empty<string>());

            var target = new UpdateActorCommandRuleSet();
            var command = new UpdateActorCommand(Guid.Empty, _validActorId, actorDto);

            // Act
            var result = await target.ValidateAsync(command).ConfigureAwait(false);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
        }

        [Fact]
        public async Task Validate_ActorId_ValidatesProperty()
        {
            // Arrange
            const string propertyName = nameof(UpdateActorCommand.ActorId);

            var actorDto = new ChangeActorDto(ValidStatus, Array.Empty<MarketRoleDto>(), Array.Empty<string>());

            var target = new UpdateActorCommandRuleSet();
            var command = new UpdateActorCommand(_validOrganizationId, Guid.Empty, actorDto);

            // Act
            var result = await target.ValidateAsync(command).ConfigureAwait(false);

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

            var actorDto = new ChangeActorDto(value, Array.Empty<MarketRoleDto>(), Array.Empty<string>());

            var target = new UpdateActorCommandRuleSet();
            var command = new UpdateActorCommand(_validOrganizationId, _validActorId, actorDto);

            // Act
            var result = await target.ValidateAsync(command).ConfigureAwait(false);

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

            var actorDto = new ChangeActorDto(ValidStatus, null!, _validMeteringPointTypes);

            var target = new UpdateActorCommandRuleSet();
            var command = new UpdateActorCommand(_validOrganizationId, _validActorId, actorDto);

            // Act
            var result = await target.ValidateAsync(command).ConfigureAwait(false);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
        }

        [Fact]
        public async Task Validate_NoMarketRoles_ValidatesProperty()
        {
            // Arrange
            const string propertyName = $"{nameof(UpdateActorCommand.ChangeActor)}.{nameof(ChangeActorDto.MarketRoles)}";

            var actorDto = new ChangeActorDto(ValidStatus, Array.Empty<MarketRoleDto>(), _validMeteringPointTypes);

            var target = new UpdateActorCommandRuleSet();
            var command = new UpdateActorCommand(_validOrganizationId, _validActorId, actorDto);

            // Act
            var result = await target.ValidateAsync(command).ConfigureAwait(false);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
        }

        [Fact]
        public async Task Validate_NullMarketRole_ValidatesProperty()
        {
            // Arrange
            const string propertyName = $"{nameof(UpdateActorCommand.ChangeActor)}.{nameof(ChangeActorDto.MarketRoles)}[0]";

            var actorDto = new ChangeActorDto(ValidStatus, new MarketRoleDto[] { null! }, _validMeteringPointTypes);

            var target = new UpdateActorCommandRuleSet();
            var command = new UpdateActorCommand(_validOrganizationId, _validActorId, actorDto);

            // Act
            var result = await target.ValidateAsync(command).ConfigureAwait(false);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
        }

        [Theory]
        [InlineData("", false)]
        [InlineData(null, false)]
        [InlineData("  ", false)]
        [InlineData("GridAccessProvider", true)]
        [InlineData("ProductionResponsibleParty", true)]
        [InlineData("Consumer", true)]
        [InlineData("CONSUMER", true)]
        [InlineData("ConsumerXyz", false)]
        public async Task Validate_MarketRoleFunction_ValidatesProperty(string value, bool isValid)
        {
            // Arrange
            const string propertyName = $"{nameof(UpdateActorCommand.ChangeActor)}.{nameof(ChangeActorDto.MarketRoles)}[0].{nameof(MarketRoleDto.Function)}";

            var actorDto = new ChangeActorDto(ValidStatus, new[] { new MarketRoleDto(value) }, Array.Empty<string>());

            var target = new UpdateActorCommandRuleSet();
            var command = new UpdateActorCommand(_validOrganizationId, _validActorId, actorDto);

            // Act
            var result = await target.ValidateAsync(command).ConfigureAwait(false);

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
            const string propertyName = $"{nameof(UpdateActorCommand.ChangeActor)}.{nameof(ChangeActorDto.MeteringPointTypes)}";

            var actorDto = new ChangeActorDto(ValidStatus, _validMarketRoles, null!);

            var target = new UpdateActorCommandRuleSet();
            var command = new UpdateActorCommand(_validOrganizationId, _validActorId, actorDto);

            // Act
            var result = await target.ValidateAsync(command).ConfigureAwait(false);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
        }

        [Fact]
        public async Task Validate_NoMeteringPoints_ValidatesProperty()
        {
            // Arrange
            const string propertyName = $"{nameof(UpdateActorCommand.ChangeActor)}.{nameof(ChangeActorDto.MeteringPointTypes)}";

            var actorDto = new ChangeActorDto(ValidStatus, _validMarketRoles, Array.Empty<string>());

            var target = new UpdateActorCommandRuleSet();
            var command = new UpdateActorCommand(_validOrganizationId, _validActorId, actorDto);

            // Act
            var result = await target.ValidateAsync(command).ConfigureAwait(false);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
        }

        [Fact]
        public async Task Validate_NullMeteringPoints_ValidatesProperty()
        {
            // Arrange
            const string propertyName = $"{nameof(UpdateActorCommand.ChangeActor)}.{nameof(ChangeActorDto.MeteringPointTypes)}[0]";

            var actorDto = new ChangeActorDto(ValidStatus, _validMarketRoles, new string[] { null! });

            var target = new UpdateActorCommandRuleSet();
            var command = new UpdateActorCommand(_validOrganizationId, _validActorId, actorDto);

            // Act
            var result = await target.ValidateAsync(command).ConfigureAwait(false);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
        }

        [Theory]
        [InlineData("", false)]
        [InlineData(null, false)]
        [InlineData("  ", false)]
        [InlineData("D06SupplyToGrid", true)]
        [InlineData("D06SUPPLYTOGRID", true)]
        [InlineData("D12TotalConsumption", true)]
        [InlineData("SupplyToGrid", false)]
        public async Task Validate_MeteringPointTypeEnum_ValidatesProperty(string value, bool isValid)
        {
            // Arrange
            const string propertyName = $"{nameof(UpdateActorCommand.ChangeActor)}.{nameof(ChangeActorDto.MeteringPointTypes)}[0]";

            var actorDto = new ChangeActorDto(ValidStatus, _validMarketRoles, new[] { value });

            var target = new UpdateActorCommandRuleSet();
            var command = new UpdateActorCommand(_validOrganizationId, _validActorId, actorDto);

            // Act
            var result = await target.ValidateAsync(command).ConfigureAwait(false);

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
