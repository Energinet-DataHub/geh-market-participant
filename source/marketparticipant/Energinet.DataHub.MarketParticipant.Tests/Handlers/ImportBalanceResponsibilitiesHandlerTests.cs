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
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.BalanceResponsibility;
using Energinet.DataHub.MarketParticipant.Application.Handlers.BalanceResponsibility;
using Energinet.DataHub.MarketParticipant.Domain;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Handlers;

[UnitTest]
public sealed class ImportBalanceResponsibilitiesHandlerTests
{
    [Fact]
    public async Task Handle_ValidBalanceResponsibleInCsv_EnqueuesAndProcesses()
    {
        // arrange
        var unitOfWork = new Mock<IUnitOfWork>();
        var unitOfWorkProvider = new Mock<IUnitOfWorkProvider>();
        unitOfWorkProvider.Setup(x => x.NewUnitOfWorkAsync()).ReturnsAsync(unitOfWork.Object);

        var balanceResponsible = CreateBalanceResponsible();

        var actorRepository = new Mock<IActorRepository>();
        actorRepository.Setup(x => x.GetActorsAsync()).ReturnsAsync([
            balanceResponsible,
        ]);

        var balanceResponsibilityRequestRepository = new Mock<IBalanceResponsibilityRequestRepository>();

        var target = new ImportBalanceResponsibilitiesHandler(unitOfWorkProvider.Object, actorRepository.Object, balanceResponsibilityRequestRepository.Object);

        var csv = "EnergySupplier,BalanceResponsible,GridArea,MeteringPointType,ValidFrom,ValidTo\n" +
                  $"6422802814540,{balanceResponsible.ActorNumber.Value},3,4,2021-01-01T00:00:00Z,2021-01-02T00:00:00Z\n";

        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        // act
        await target.Handle(new ImportBalanceResponsibilitiesCommand(ms), default);

        // assert
        balanceResponsibilityRequestRepository.Verify(x => x.EnqueueAsync(It.Is<BalanceResponsibilityRequest>(y => y.BalanceResponsibleParty == balanceResponsible.ActorNumber)), Times.Once);
        balanceResponsibilityRequestRepository.Verify(x => x.ProcessNextRequestsAsync(It.Is<ActorId>(y => y == balanceResponsible.Id)), Times.Once);
        unitOfWork.Verify(x => x.CommitAsync(), Times.Once);
        unitOfWork.Verify(x => x.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task Handle_BalanceResponsibleNotFound_ThrowsValidationExceptionAndRollsBack()
    {
        // arrange
        var unitOfWork = new Mock<IUnitOfWork>();
        var unitOfWorkProvider = new Mock<IUnitOfWorkProvider>();
        unitOfWorkProvider.Setup(x => x.NewUnitOfWorkAsync()).ReturnsAsync(unitOfWork.Object);

        var actorRepository = new Mock<IActorRepository>();
        var balanceResponsibilityRequestRepository = new Mock<IBalanceResponsibilityRequestRepository>();

        var balanceResponsible = "6422802814540";

        var target = new ImportBalanceResponsibilitiesHandler(unitOfWorkProvider.Object, actorRepository.Object, balanceResponsibilityRequestRepository.Object);

        var csv = "EnergySupplier,BalanceResponsible,GridArea,MeteringPointType,ValidFrom,ValidTo\n" +
                  $"6422802814540,{balanceResponsible},3,4,2021-01-01T00:00:00Z,2021-01-02T00:00:00Z\n";

        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        // act
        var actual = await Assert.ThrowsAsync<ValidationException>(() => target.Handle(new ImportBalanceResponsibilitiesCommand(ms), default));

        // assert
        Assert.Contains(balanceResponsible, actual.Message, StringComparison.InvariantCultureIgnoreCase);
        unitOfWork.Verify(x => x.CommitAsync(), Times.Never);
        unitOfWork.Verify(x => x.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task Handle_ProcessThrows_RollsBack()
    {
        // arrange
        var unitOfWork = new Mock<IUnitOfWork>();
        var unitOfWorkProvider = new Mock<IUnitOfWorkProvider>();
        unitOfWorkProvider.Setup(x => x.NewUnitOfWorkAsync()).ReturnsAsync(unitOfWork.Object);

        var balanceResponsible = CreateBalanceResponsible();

        var actorRepository = new Mock<IActorRepository>();
        actorRepository.Setup(x => x.GetActorsAsync()).ReturnsAsync([
            balanceResponsible,
        ]);

        var balanceResponsibilityRequestRepository = new Mock<IBalanceResponsibilityRequestRepository>();
        balanceResponsibilityRequestRepository.Setup(x => x.ProcessNextRequestsAsync(balanceResponsible.Id)).ThrowsAsync(new InvalidOperationException());

        var target = new ImportBalanceResponsibilitiesHandler(unitOfWorkProvider.Object, actorRepository.Object, balanceResponsibilityRequestRepository.Object);

        var csv = "EnergySupplier,BalanceResponsible,GridArea,MeteringPointType,ValidFrom,ValidTo\n" +
                  $"6422802814540,{balanceResponsible.ActorNumber.Value},3,4,2021-01-01T00:00:00Z,2021-01-02T00:00:00Z\n";

        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        // act
        await Assert.ThrowsAsync<InvalidOperationException>(() => target.Handle(new ImportBalanceResponsibilitiesCommand(ms), default));

        // assert
        unitOfWork.Verify(x => x.CommitAsync(), Times.Never);
        unitOfWork.Verify(x => x.DisposeAsync(), Times.Once);
    }

    private static Actor CreateBalanceResponsible()
    {
        return new Actor(
            new ActorId(Guid.NewGuid()),
            new OrganizationId(Guid.NewGuid()),
            null,
            new GlnActorNumber("3813271387117"),
            ActorStatus.Active,
            [new ActorMarketRole(EicFunction.BalanceResponsibleParty)],
            new ActorName(Guid.NewGuid().ToString()),
            null);
    }
}
