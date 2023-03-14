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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.Core.App.Common.Security;
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoles;
using Energinet.DataHub.MarketParticipant.Application.Handlers.UserRoles;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Handlers
{
    [UnitTest]
    public sealed class GetUserRoleToPermissionHandlerTest
    {
        [Fact]
        public async Task HandleCommandCallsRepositoryAsync()
        {
            // arrange
            var repositoryMock = new Mock<IUserRoleRepository>();
            repositoryMock
                .Setup(x => x.GetAsync(It.IsAny<Permission>()))
                .ReturnsAsync(Array.Empty<UserRole>());

            var target = new GetUserRolesToPermissionHandler(repositoryMock.Object);

            // act
            var actual = await target.Handle(new GetUserRolesToPermissionCommand((int)Permission.UserRoleManage), CancellationToken.None).ConfigureAwait(false);

            // assert
            Assert.NotNull(actual.UserRoles);
        }
    }
}
