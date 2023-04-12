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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Permissions;
using Energinet.DataHub.MarketParticipant.Application.Handlers.Permissions;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Handlers
{
    [UnitTest]
    public sealed class GetPermissionHandlerTests
    {
        [Fact]
        public async Task HandleCommandCallsRepositoryAsync()
        {
            // arrange
            var repositoryMock = new Mock<IPermissionRepository>();
            var permissionMock = new Permission(PermissionId.ActorsManage, "ActorsManage", "ActorsManage", NodaTime.Instant.MaxValue, new List<EicFunction>() { EicFunction.BalanceResponsibleParty });

            repositoryMock
                .Setup(x => x.GetAsync(PermissionId.ActorsManage))
                .ReturnsAsync(permissionMock);

            var target = new GetPermissionsHandler(repositoryMock.Object);

            // act
            var actual = await target.Handle(new GetPermissionsCommand(), CancellationToken.None).ConfigureAwait(false);

            // assert
            Assert.NotNull(actual.Permissions);
        }
    }
}
