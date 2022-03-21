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
using Energinet.DataHub.MarketParticipant.Integration.Model.Dtos;
using Energinet.DataHub.MarketParticipant.Integration.Model.Parsers;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Libraries.Tests.Parsers
{
    [UnitTest]
    public class OrganizationChangedEventParserTests
    {
        [Fact]
        public void Parse_InputValid_ParsesCorrectly()
        {
            // arrange
            var target = new ActorUpdatedIntegrationEventParser();
            var @event = new ActorUpdatedIntegrationEvent(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "0123456789012",
                ActorStatus.Active,
                new[] { BusinessRoleCode.Ddk, BusinessRoleCode.Ddm },
                new[] { EicFunction.Agent, EicFunction.BalanceResponsibleParty });

            // act
            var actualBytes = target.Parse(@event);
            var actualEvent = target.Parse(actualBytes);

            // assert
            Assert.Equal(@event.Id, actualEvent.Id);
            Assert.Equal(@event.ActorId, actualEvent.ActorId);
            Assert.Equal(@event.ExternalActorId, actualEvent.ExternalActorId);
            Assert.Equal(@event.OrganizationId, actualEvent.OrganizationId);
            Assert.Equal(@event.Gln, actualEvent.Gln);
            Assert.Equal(@event.Status, actualEvent.Status);
            Assert.Equal(@event.BusinessRoles, actualEvent.BusinessRoles);
            Assert.Equal(@event.MarketRoles, actualEvent.MarketRoles);
        }
    }
}
