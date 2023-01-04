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

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoles;
using Energinet.DataHub.MarketParticipant.Application.Validation;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Validation
{
    [UnitTest]
    public sealed class CreateUserRoleCommandRuleSetTests
    {
        private const string ValidName = "Support";
        private const string ValidDescription = "This is the support role";
        private const string ValidStatus = "Active";
        private const string ValidEicFunction = "EnergySupplier";
        private const string ValidPermission = nameof(Core.App.Common.Security.Permission.ActorManage);

        [Fact]
        public async Task Validate_UserRole_ValidatesProperty()
        {
            // Arrange
            const string propertyName = nameof(CreateUserRoleCommand.UserRoleDto);

            var target = new CreateUserRoleCommandRuleSet();
            var command = new CreateUserRoleCommand(null!);

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
        [InlineData(ValidName, true)]
        [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", true)]
        [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaX", false)]
        public async Task Validate_Name_ValidatesProperty(string value, bool isValid)
        {
            // Arrange
            var propertyName = $"{nameof(CreateUserRoleCommand.UserRoleDto)}.{nameof(CreateUserRoleDto.Name)}";

            var createGridAreaDto = new CreateUserRoleDto(
                value,
                ValidDescription,
                ValidStatus,
                ValidEicFunction,
                new Collection<string> { ValidPermission });

            var target = new CreateUserRoleCommandRuleSet();
            var command = new CreateUserRoleCommand(createGridAreaDto);

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
        [InlineData(ValidStatus, true)]
        [InlineData("fake_value", false)]
        public async Task Validate_Status_ValidatesProperty(string value, bool isValid)
        {
            // Arrange
            var propertyName = $"{nameof(CreateUserRoleCommand.UserRoleDto)}.{nameof(CreateUserRoleDto.Status)}";

            var createGridAreaDto = new CreateUserRoleDto(
                ValidName,
                ValidDescription,
                value,
                ValidEicFunction,
                new Collection<string> { ValidPermission });

            var target = new CreateUserRoleCommandRuleSet();
            var command = new CreateUserRoleCommand(createGridAreaDto);

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
        [InlineData(ValidEicFunction, true)]
        [InlineData("fake_value", false)]
        public async Task Validate_EicFunction_ValidatesProperty(string value, bool isValid)
        {
            // Arrange
            var propertyName = $"{nameof(CreateUserRoleCommand.UserRoleDto)}.{nameof(CreateUserRoleDto.EicFunction)}";

            var createGridAreaDto = new CreateUserRoleDto(
                ValidName,
                ValidDescription,
                ValidStatus,
                value,
                new Collection<string> { ValidPermission });

            var target = new CreateUserRoleCommandRuleSet();
            var command = new CreateUserRoleCommand(createGridAreaDto);

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
        [InlineData(ValidPermission, true)]
        [InlineData("fake_value", false)]
        public async Task Validate_Permissions_ValidatesProperty(string value, bool isValid)
        {
            // Arrange
            var propertyName = $"{nameof(CreateUserRoleCommand.UserRoleDto)}.{nameof(CreateUserRoleDto.Permissions)}[0]";

            var createGridAreaDto = new CreateUserRoleDto(
                ValidName,
                ValidDescription,
                ValidStatus,
                ValidEicFunction,
                new Collection<string>() { value });

            var target = new CreateUserRoleCommandRuleSet();
            var command = new CreateUserRoleCommand(createGridAreaDto);

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
