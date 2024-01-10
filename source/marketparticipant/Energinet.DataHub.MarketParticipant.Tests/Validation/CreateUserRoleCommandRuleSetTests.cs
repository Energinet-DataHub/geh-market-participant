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

using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoles;
using Energinet.DataHub.MarketParticipant.Application.Validation;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Validation
{
    [UnitTest]
    public sealed class CreateUserRoleCommandRuleSetTests
    {
        private const string ValidName = "Support";
        private const string ValidDescription = "This is the support role";
        private const UserRoleStatus ValidStatus = UserRoleStatus.Active;
        private const EicFunction ValidEicFunction = EicFunction.EnergySupplier;
        private const int ValidPermission = (int)PermissionId.ActorsManage;

        [Fact]
        public async Task Validate_UserRole_ValidatesProperty()
        {
            // Arrange
            const string propertyName = nameof(CreateUserRoleCommand.UserRoleDto);

            var target = new CreateUserRoleCommandRuleSet();
            var command = new CreateUserRoleCommand(null!);

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
        [InlineData(ValidName, true)]
        [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", true)]
        [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaX", false)]
        public async Task Validate_Name_ValidatesProperty(string? value, bool isValid)
        {
            // Arrange
            const string propertyName = $"{nameof(CreateUserRoleCommand.UserRoleDto)}.{nameof(CreateUserRoleDto.Name)}";

            var createGridAreaDto = new CreateUserRoleDto(
                value!,
                ValidDescription,
                ValidStatus,
                ValidEicFunction,
                new Collection<int> { ValidPermission });

            var target = new CreateUserRoleCommandRuleSet();
            var command = new CreateUserRoleCommand(createGridAreaDto);

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

        [Theory]
        [InlineData((int)UserRoleStatus.Active, true)]
        [InlineData((int)UserRoleStatus.Inactive, true)]
        [InlineData(12, false)]
        public async Task Validate_Status_ValidatesProperty(int value, bool isValid)
        {
            // Arrange
            const string propertyName = $"{nameof(CreateUserRoleCommand.UserRoleDto)}.{nameof(CreateUserRoleDto.Status)}";

            var createGridAreaDto = new CreateUserRoleDto(
                ValidName,
                ValidDescription,
                (UserRoleStatus)value,
                ValidEicFunction,
                new Collection<int> { ValidPermission });

            var target = new CreateUserRoleCommandRuleSet();
            var command = new CreateUserRoleCommand(createGridAreaDto);

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

        [Theory]
        [InlineData((int)EicFunction.EnergySupplier, true)]
        [InlineData(2550, false)]
        public async Task Validate_EicFunction_ValidatesProperty(int value, bool isValid)
        {
            // Arrange
            const string propertyName = $"{nameof(CreateUserRoleCommand.UserRoleDto)}.{nameof(CreateUserRoleDto.EicFunction)}";

            var createGridAreaDto = new CreateUserRoleDto(
                ValidName,
                ValidDescription,
                ValidStatus,
                (EicFunction)value,
                new Collection<int> { ValidPermission });

            var target = new CreateUserRoleCommandRuleSet();
            var command = new CreateUserRoleCommand(createGridAreaDto);

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

        [Theory]
        [InlineData(-1, false)]
        [InlineData(0, false)]
        [InlineData(int.MinValue, false)]
        [InlineData(ValidPermission, true)]
        [InlineData(int.MaxValue, false)]
        public async Task Validate_Permissions_ValidatesProperty(int value, bool isValid)
        {
            // Arrange
            const string propertyName = $"{nameof(CreateUserRoleCommand.UserRoleDto)}.{nameof(CreateUserRoleDto.Permissions)}[0]";

            var createGridAreaDto = new CreateUserRoleDto(
                ValidName,
                ValidDescription,
                ValidStatus,
                ValidEicFunction,
                new Collection<int>() { value });

            var target = new CreateUserRoleCommandRuleSet();
            var command = new CreateUserRoleCommand(createGridAreaDto);

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
