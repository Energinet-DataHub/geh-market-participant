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
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users.Authentication;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Moq;
using Xunit;
using Xunit.Categories;
using RequiredPermissionForUserRoleRuleService = Energinet.DataHub.MarketParticipant.Domain.Services.Rules.RequiredPermissionForUserRoleRuleService;

namespace Energinet.DataHub.MarketParticipant.Tests.Services;

[UnitTest]
public sealed class RequiredPermissionForUserRoleRuleServiceTests
{
    private readonly HashSet<(PermissionId Permission, EicFunction MarketRole)> _actualCombos =
        (HashSet<(PermissionId Permission, EicFunction MarketRole)>)typeof(RequiredPermissionForUserRoleRuleService).GetField("_requiredPermissions", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;

    private readonly HashSet<(PermissionId Permission, EicFunction MarketRole)> _excpectedCombos =
    [
        (PermissionId.UsersManage, EicFunction.DataHubAdministrator),
        (PermissionId.UserRolesManage, EicFunction.DataHubAdministrator),
    ];

    [Theory]
    [InlineData(null, null, false, true)]
    [InlineData(UserRoleStatus.Active, null, false, true)]
    [InlineData(UserRoleStatus.Inactive, UserIdentityStatus.Active, false, true)]
    [InlineData(UserRoleStatus.Active, UserIdentityStatus.Inactive, false, true)]
    [InlineData(UserRoleStatus.Active, UserIdentityStatus.Active, true, true)]
    [InlineData(UserRoleStatus.Active, UserIdentityStatus.Active, false, false)]
    public async Task Validate_Called_ThrowsWhenExpected(UserRoleStatus? userRoleStatus, UserIdentityStatus? userStatus, bool excludeUser, bool shouldThrow)
    {
        // arrange
        var userRoleRepository = new Mock<IUserRoleRepository>();
        var userRepository = new Mock<IUserRepository>();
        var userIdentityRepository = new Mock<IUserIdentityRepository>();

        var userRoleId = new UserRoleId(Guid.NewGuid());
        var userId = new UserId(Guid.NewGuid());

        foreach (var (permissionId, marketRole) in _excpectedCombos)
        {
            if (userRoleStatus is { } urs)
            {
                var userRole = new UserRole(userRoleId, "User Role Name", "User Role Description", urs, [permissionId], marketRole);
                userRoleRepository
                    .Setup(x => x.GetAsync(permissionId))
                    .ReturnsAsync([userRole]);

                if (userStatus is { } uis)
                {
                    var user = new User(userId, new ActorId(Guid.NewGuid()), new ExternalUserId(Guid.NewGuid()), [], null, null, null);
                    userRepository
                        .Setup(x => x.GetToUserRoleAsync(userRole.Id))
                        .ReturnsAsync([user]);

                    var userIdentity = new UserIdentity(user.ExternalId, new EmailAddress("foo@bar.baz"), uis, "firstname", "lastname", null, DateTimeOffset.Now, AuthenticationMethod.Undetermined, []);
                    userIdentityRepository
                        .Setup(x => x.GetAsync(user.ExternalId))
                        .ReturnsAsync(userIdentity);
                }
            }
        }

        var target = new RequiredPermissionForUserRoleRuleService(userRoleRepository.Object, userRepository.Object, userIdentityRepository.Object);

        // act + assert
        Assert.Equal(_excpectedCombos, _actualCombos);
        if (shouldThrow)
        {
            await Assert.ThrowsAsync<ValidationException>(() => target.ValidateExistsAsync(excludeUser ? [userId] : []));
        }
        else
        {
            await target.ValidateExistsAsync(excludeUser ? [userId] : []);
        }
    }
}
