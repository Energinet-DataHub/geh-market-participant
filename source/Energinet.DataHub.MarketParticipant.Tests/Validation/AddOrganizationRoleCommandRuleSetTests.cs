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
using Energinet.DataHub.MarketParticipant.Application.Validation;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Validation
{
    [UnitTest]
    public sealed class AddOrganizationRoleCommandRuleSetTests
    {
        private const string ValidId = "6AF7D019-06A7-465B-AF9E-983BF0C7A907";
        private const string ValidRole = "DDQ";

        [Fact]
        public async Task Validate_OrganizationRoleDto_ValidatesProperty()
        {
            // Arrange
            const string propertyName = nameof(AddOrganizationRoleCommand.Role);

            var target = new AddOrganizationRoleCommandRuleSet();
            var command = new AddOrganizationRoleCommand(ValidId, null!);

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
            const string propertyName = nameof(AddOrganizationRoleCommand.OrganizationId);

            var organizationRoleDto = new OrganizationRoleDto(ValidRole);

            var target = new AddOrganizationRoleCommandRuleSet();
            var command = new AddOrganizationRoleCommand(value, organizationRoleDto);

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
        [InlineData("DDM", true)]
        [InlineData("Ddm", true)]
        [InlineData("ddm", true)]
        [InlineData("DDK", true)]
        [InlineData("DDQ", true)]
        [InlineData("DDX", true)]
        [InlineData("DDZ", true)]
        [InlineData("DEA", true)]
        [InlineData("MDR", true)]
        [InlineData("STS", true)]
        [InlineData("EZ", true)]
        [InlineData("ddd", false)]
        [InlineData("DDXyz", false)]
        public async Task Validate_BusinessRole_ValidatesProperty(string value, bool isValid)
        {
            // Arrange
            var propertyName = $"{nameof(AddOrganizationRoleCommand.Role)}.{nameof(OrganizationRoleDto.BusinessRole)}";

            var organizationRoleDto = new OrganizationRoleDto(value);

            var target = new AddOrganizationRoleCommandRuleSet();
            var command = new AddOrganizationRoleCommand(ValidId, organizationRoleDto);

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
