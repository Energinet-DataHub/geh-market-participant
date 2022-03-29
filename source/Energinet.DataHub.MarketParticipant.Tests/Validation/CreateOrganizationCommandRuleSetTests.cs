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

using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands;
using Energinet.DataHub.MarketParticipant.Application.Commands.Organization;
using Energinet.DataHub.MarketParticipant.Application.Validation;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Validation
{
    [UnitTest]
    public sealed class CreateOrganizationCommandRuleSetTests
    {
        [Fact]
        public async Task Validate_OrganizationDto_ValidatesProperty()
        {
            // Arrange
            const string propertyName = nameof(CreateOrganizationCommand.Organization);

            var target = new CreateOrganizationCommandRuleSet();
            var command = new CreateOrganizationCommand(null!);

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
        [InlineData("Some Name", true)]
        [InlineData("Maximum looooooooooooooooooooooooooooooooooooooong", true)]
        [InlineData("Toooooo loooooooooooooooooooooooooooooooooooooooong", false)]
        public async Task Validate_OrganizationName_ValidatesProperty(string value, bool isValid)
        {
            // Arrange
            var propertyName = $"{nameof(CreateOrganizationCommand.Organization)}.{nameof(ChangeOrganizationDto.Name)}";

            var organizationDto = new ChangeOrganizationDto(value);

            var target = new CreateOrganizationCommandRuleSet();
            var command = new CreateOrganizationCommand(organizationDto);

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
