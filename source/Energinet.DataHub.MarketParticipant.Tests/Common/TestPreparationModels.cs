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
using Energinet.DataHub.MarketParticipant.Domain.Model;

namespace Energinet.DataHub.MarketParticipant.Tests.Common;

internal static class TestPreparationModels
{
    public static Organization MockedOrganization() => MockedOrganization(Guid.NewGuid());

    public static Organization MockedOrganization(Guid organizationId) => new(
        new OrganizationId(organizationId),
        "Animal Power Company",
        MockedBusinessRegisterIdentifier.New(),
        new Address("Vej Allé", "7", "7100", "Vejle", "DK"),
        new MockedDomain(),
        null,
        OrganizationStatus.Active);

    public static Actor MockedActor() => MockedActor(Guid.NewGuid(), Guid.NewGuid());

    public static Actor MockedActor(Guid actorId) => MockedActor(actorId, Guid.NewGuid());

    public static Actor MockedActor(Guid actorId, Guid organizationId) => new(
        new ActorId(actorId),
        new OrganizationId(organizationId),
        new ExternalActorId(Guid.NewGuid()),
        new MockedGln(),
        ActorStatus.Active,
        new[] { new ActorMarketRole(EicFunction.GridAccessProvider) },
        new ActorName("Sloth Power"));
}
