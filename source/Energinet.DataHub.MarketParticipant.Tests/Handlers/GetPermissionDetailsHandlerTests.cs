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
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoles;
using Energinet.DataHub.MarketParticipant.Application.Handlers.GridArea;
using Energinet.DataHub.MarketParticipant.Application.Handlers.UserRoles;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Handlers
{
    [UnitTest]
    public sealed class GetPermissionDetailsHandlerTests
    {
        [Fact]
        public async Task Handle_NullArgument_ThrowsException()
        {
            // arrange
            var target = new GetPermissionDetailsHandler(new Mock<IPermissionRepository>().Object);

            // act assert
            await Assert
                .ThrowsAsync<ArgumentNullException>(() => target.Handle(null!, CancellationToken.None))
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task Handle_Command_CallsRepository()
        {
            // arrange
            var repositoryMock = new Mock<IPermissionRepository>();
            repositoryMock
                .Setup(x => x.GetToMarketRoleAsync(It.IsAny<EicFunction>()))
                .ReturnsAsync(Array.Empty<PermissionDetails>());

            var target = new GetPermissionDetailsHandler(repositoryMock.Object);

            // act
            var actual = await target.Handle(new GetPermissionDetailsCommand(EicFunction.BillingAgent), CancellationToken.None).ConfigureAwait(false);

            // assert
            Assert.NotNull(actual.Permissions);
        }
    }
}
