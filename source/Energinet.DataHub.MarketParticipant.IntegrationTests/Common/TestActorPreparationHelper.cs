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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Common;

internal static class TestActorPreparationHelper
{
    public static Task<ActorEntity> PrepareActorAsync(
        this MarketParticipantDatabaseFixture fixture)
    {
        return fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActor,
            TestPreparationEntities.ValidMarketRole);
    }

    public static async Task<ActorEntity> PrepareActorAsync(
        this MarketParticipantDatabaseFixture fixture,
        OrganizationEntity inputOrganization,
        ActorEntity inputActor,
        params MarketRoleEntity[] inputMarketRoles)
    {
        await using var context = fixture.DatabaseManager.CreateDbContext();

        foreach (var localMarketRole in inputMarketRoles)
        {
            inputActor.MarketRoles.Add(localMarketRole);
        }

        if (inputOrganization.Id == Guid.Empty)
        {
            await context.Organizations.AddAsync(inputOrganization);
            await context.SaveChangesAsync();
        }

        inputActor.OrganizationId = inputOrganization.Id;

        await context.Actors.AddAsync(inputActor);
        await context.SaveChangesAsync();

        return inputActor;
    }
}
