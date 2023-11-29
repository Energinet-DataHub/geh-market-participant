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
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services.Rules;
using Energinet.DataHub.MarketParticipant.Tests.Common;
using Moq;
using Xunit;

namespace Energinet.DataHub.MarketParticipant.Tests.Services
{
    public sealed class UniqueMarketRoleGridAreaRuleServiceTests
    {
        [Fact]
        public async Task Ensure_ActorSupplied_CallsRemoveForActor()
        {
            // arrange
            var repository = new Mock<IMarketRoleAndGridAreaForActorReservationService>();

            var target = new UniqueMarketRoleGridAreaRuleService(repository.Object);

            var actor = new Actor(
                new ActorId(Guid.NewGuid()),
                new OrganizationId(Guid.NewGuid()),
                null,
                new MockedGln(),
                ActorStatus.Active,
                new[]
                {
                    new ActorMarketRole(
                        EicFunction.GridAccessProvider,
                        Enumerable.Empty<ActorGridArea>())
                },
                new ActorName("fake_value"),
                null);

            // act
            await target.ValidateAndReserveAsync(actor);

            // assert
            repository.Verify(x => x.RemoveAllReservationsAsync(actor.Id), Times.Exactly(1));
        }

        [Fact]
        public async Task Ensure_ActorSupplied_CallsTryAddForMarketRoleGridAreas()
        {
            // arrange
            var repository = new Mock<IMarketRoleAndGridAreaForActorReservationService>();
            repository
                .Setup(x => x.TryReserveAsync(It.IsAny<ActorId>(), EicFunction.GridAccessProvider, It.IsAny<GridAreaId>()))
                .ReturnsAsync(true);

            var target = new UniqueMarketRoleGridAreaRuleService(repository.Object);

            var actor = new Actor(
                new ActorId(Guid.NewGuid()),
                new OrganizationId(Guid.NewGuid()),
                null,
                new MockedGln(),
                ActorStatus.Active,
                new[]
                {
                    new ActorMarketRole(
                        EicFunction.GridAccessProvider,
                        new[]
                        {
                            new ActorGridArea(
                                new GridAreaId(Guid.NewGuid()),
                                new[] { MeteringPointType.D02Analysis })
                        })
                },
                new ActorName("fake_value"),
                null);

            // act
            await target.ValidateAndReserveAsync(actor);

            // assert
            foreach (var mr in actor.MarketRoles)
            {
                foreach (var ga in mr.GridAreas)
                {
                    repository.Verify(x => x.TryReserveAsync(actor.Id, mr.Function, ga.Id), Times.Exactly(1));
                }
            }
        }

        [Fact]
        public async Task Ensure_NonDdmMarketRole_DoesntEnsureUniqueness()
        {
            foreach (var nonDdmMarketRole in Enum.GetValues<EicFunction>().Except(
                new[]
                {
                    EicFunction.GridAccessProvider
                }))
            {
                // arrange
                var repository = new Mock<IMarketRoleAndGridAreaForActorReservationService>();

                var target = new UniqueMarketRoleGridAreaRuleService(repository.Object);

                var actor = new Actor(
                    new ActorId(Guid.NewGuid()),
                    new OrganizationId(Guid.NewGuid()),
                    null,
                    new MockedGln(),
                    ActorStatus.Active,
                    new[]
                    {
                    new ActorMarketRole(
                        nonDdmMarketRole,
                        Enumerable.Empty<ActorGridArea>())
                    },
                    new ActorName("fake_value"),
                    null);

                // act
                await target.ValidateAndReserveAsync(actor);

                // assert
                repository.Verify(x => x.TryReserveAsync(It.IsAny<ActorId>(), It.IsAny<EicFunction>(), It.IsAny<GridAreaId>()), Times.Never);
            }
        }
    }
}
