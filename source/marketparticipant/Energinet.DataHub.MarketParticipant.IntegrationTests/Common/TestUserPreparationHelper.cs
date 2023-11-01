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

internal static class TestUserPreparationHelper
{
    public static Task<UserEntity> PrepareUserAsync(this MarketParticipantDatabaseFixture fixture)
    {
        return fixture.PrepareUserAsync(TestPreparationEntities.UnconnectedUser);
    }

    public static async Task<UserEntity> PrepareUserAsync(
        this MarketParticipantDatabaseFixture fixture,
        UserEntity userEntity)
    {
        await using var context = fixture.DatabaseManager.CreateDbContext();

        if (userEntity.AdministratedByActorId == Guid.Empty)
        {
            var actor = await fixture.PrepareActorAsync();
            userEntity.AdministratedByActorId = actor.Id;
        }

        await context.Users.AddAsync(userEntity);
        await context.SaveChangesAsync();

        return userEntity;
    }

    public static async Task AssignUserRoleAsync(
        this MarketParticipantDatabaseFixture fixture,
        Guid userId,
        Guid actorId,
        Guid userRoleId)
    {
        await using var context = fixture.DatabaseManager.CreateDbContext();

        var roleAssignment = new UserRoleAssignmentEntity
        {
            ActorId = actorId,
            UserRoleId = userRoleId
        };

        var userEntity = await context.Users.FindAsync(userId);
        userEntity!.RoleAssignments.Add(roleAssignment);

        context.Users.Update(userEntity);
        await context.SaveChangesAsync();
    }

    public static async Task AssignActorCredentialsAsync(
            this MarketParticipantDatabaseFixture fixture,
            Guid actorId,
            string certificateThumbprint,
            string certificateLookupIdentifier)
        {
            await using var context = fixture.DatabaseManager.CreateDbContext();

            var actorCertificateCredentials = new ActorCertificateCredentialsEntity
            {
                ActorId = actorId,
                CertificateThumbprint = certificateThumbprint,
                KeyVaultSecretIdentifier = certificateLookupIdentifier
            };

            var actorEntity = await context.Actors.FindAsync(actorId);
            actorEntity!.CertificateCredential = actorCertificateCredentials;

            context.Actors.Update(actorEntity);
            await context.SaveChangesAsync();
        }
}
