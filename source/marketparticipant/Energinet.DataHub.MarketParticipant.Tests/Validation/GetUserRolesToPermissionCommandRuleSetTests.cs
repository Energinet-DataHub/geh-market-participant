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
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoles;
using Energinet.DataHub.MarketParticipant.Application.Validation;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Validation
{
    [UnitTest]
    public sealed class GetUserRolesToPermissionCommandRuleSetTests
    {
        [Fact]
        public async Task Validate_PermissionId_ValidatesInvalidProperty()
        {
            // Arrange
            const string propertyName = nameof(GetUserRolesToPermissionCommand.PermissionId);

            var target = new GetUserRolesToPermissionCommandRuleSet();
            var command = new GetUserRolesToPermissionCommand(-1);

            // Act
            var result = await target.ValidateAsync(command);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
        }

        [Fact]
        public async Task Validate_PermissionId_ValidatesProperty()
        {
            // Arrange
            var target = new GetUserRolesToPermissionCommandRuleSet();
            var command = new GetUserRolesToPermissionCommand((int)PermissionId.OrganizationsManage);

            // Act
            var result = await target.ValidateAsync(command);

            // Assert
            Assert.True(result.IsValid);
        }
    }
}
