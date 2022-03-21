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
using Energinet.DataHub.MarketParticipant.Application.Commands;
using Energinet.DataHub.MarketParticipant.Application.Validation;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Validation
{
    [UnitTest]
    public sealed class CreateActorCommandRuleSetTests
    {
        private const string ValidId = "6AF7D019-06A7-465B-AF9E-983BF0C7A907";
        private const string ValidGln = "5790000555550";

        [Fact]
        public async Task Validate_ActorDto_ValidatesProperty()
        {
            // Arrange
            const string propertyName = nameof(CreateActorCommand.CreateActor);

            var target = new CreateActorCommandRuleSet();
            var command = new CreateActorCommand(ValidId, null!);

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
        [InlineData("8F9B8218-BAE6-412B-B91B-0C78A55FF128", true)]
        [InlineData("8F9B8218-BAE6-412B-B91B-0C78A55FF1XX", false)]
        public async Task Validate_OrganizationId_ValidatesProperty(string value, bool isValid)
        {
            // Arrange
            const string propertyName = nameof(CreateActorCommand.OrganizationId);

            var actorDto = new CreateActorDto(new GlobalLocationNumberDto(ValidGln), Array.Empty<MarketRoleDto>());

            var target = new CreateActorCommandRuleSet();
            var command = new CreateActorCommand(value, actorDto);

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

        [Theory]
        [InlineData("", false)]
        [InlineData(null, false)]
        [InlineData("  ", false)]
        [InlineData("5790000555550", true)]
        public async Task Validate_ActorGln_ValidatesProperty(string value, bool isValid)
        {
            // Arrange
            var propertyName = $"{nameof(CreateActorCommand.CreateActor)}.{nameof(CreateActorDto.Gln)}";

            var actorDto = new CreateActorDto(new GlobalLocationNumberDto(value), Array.Empty<MarketRoleDto>());

            var target = new CreateActorCommandRuleSet();
            var command = new CreateActorCommand(ValidId, actorDto);

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
            var propertyName = $"{nameof(CreateActorCommand.CreateActor)}.{nameof(CreateActorDto.MarketRoles)}";

            var organizationRoleDto = new CreateActorDto(new GlobalLocationNumberDto(ValidGln), null!);

            var target = new CreateActorCommandRuleSet();
            var command = new CreateActorCommand(ValidId, organizationRoleDto);

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
            var propertyName = $"{nameof(CreateActorCommand.CreateActor)}.{nameof(CreateActorDto.MarketRoles)}[0]";

            var organizationRoleDto = new CreateActorDto(new GlobalLocationNumberDto(ValidGln), new MarketRoleDto[] { null! });

            var target = new CreateActorCommandRuleSet();
            var command = new CreateActorCommand(ValidId, organizationRoleDto);

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
            var propertyName = $"{nameof(CreateActorCommand.CreateActor)}.{nameof(CreateActorDto.MarketRoles)}[0].{nameof(MarketRoleDto.Function)}";

            var organizationRoleDto = new CreateActorDto(new GlobalLocationNumberDto(ValidGln), new[] { new MarketRoleDto(value) });

            var target = new CreateActorCommandRuleSet();
            var command = new CreateActorCommand(ValidId, organizationRoleDto);

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
