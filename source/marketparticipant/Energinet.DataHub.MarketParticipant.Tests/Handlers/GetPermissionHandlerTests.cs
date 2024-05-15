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

namespace Energinet.DataHub.MarketParticipant.Tests.Handlers;

[UnitTest]
public sealed class GetPermissionHandlerTests
{
    [Fact]
    public async Task Handle_ValidPermission_ReturnsPermission()
    {
        // arrange
        var repositoryMock = new Mock<IPermissionRepository>();
        var permissionMock = new Permission(
            PermissionId.ActorsManage,
            "ActorsManage",
            "ActorsManage",
            NodaTime.Instant.MaxValue,
            new[] { EicFunction.BalanceResponsibleParty });

        repositoryMock
            .Setup(x => x.GetAsync(PermissionId.ActorsManage))
            .ReturnsAsync(permissionMock);

        var target = new GetPermissionHandler(repositoryMock.Object);
        var command = new GetPermissionCommand((int)PermissionId.ActorsManage);

        // act
        var actual = await target.Handle(command, CancellationToken.None);

        // assert
        Assert.NotNull(actual);
        Assert.Equal(permissionMock.Id, (PermissionId)actual.Permission.Id);
        Assert.Equal(permissionMock.Name, actual.Permission.Name);
        Assert.Equal(permissionMock.Description, actual.Permission.Description);
        Assert.Equal(permissionMock.Created.ToDateTimeOffset(), actual.Permission.Created);
        Assert.Single(actual.Permission.AssignableTo, EicFunction.BalanceResponsibleParty);
    }
}
