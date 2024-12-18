﻿// Copyright 2020 Energinet DataHub A/S
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
using System.Globalization;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Common;

internal static class TestPreparationEntities
{
    // Grid Areas starts at 10 because '003' and '007' are reserved and should not "occur" randomly as tests are run.
    private static int _gridAreaCount = 10;

    public static OrganizationEntity ValidOrganization => new()
    {
        Name = "Test Organization Name",
        BusinessRegisterIdentifier = MockedBusinessRegisterIdentifier.New().Identifier,
        Domains =
        {
            new OrganizationDomainEntity
            {
                Domain = new MockedDomain()
            }
        },
        Status = 1,
        StreetName = "Vej Allé",
        Number = "7",
        City = "Vejle",
        Country = "DK",
        ZipCode = "7100"
    };

    public static ActorEntity ValidActor => new()
    {
        Id = Guid.NewGuid(),
        ActorNumber = new MockedGln(),
        ActorId = null,
        IsFas = false,
        Name = "Test Actor Name",
        Status = ActorStatus.New,
        MarketRole = new MarketRoleEntity
        {
            Function = EicFunction.BillingAgent,
        },
    };

    public static ActorEntity ValidActiveActor => new()
    {
        Id = Guid.NewGuid(),
        ActorNumber = new MockedGln(),
        ActorId = null,
        IsFas = false,
        Name = "Test Actor Name",
        Status = ActorStatus.Active
    };

    public static MarketRoleEntity ValidMarketRole => new()
    {
        Function = EicFunction.GridAccessProvider,
        Comment = "Test Market Role Comment"
    };

    public static UserEntity UnconnectedUser => new()
    {
        Email = new RandomlyGeneratedEmailAddress(),
        ExternalId = Guid.NewGuid()
    };

    public static UserRoleEntity ValidUserRole => new()
    {
        Name = "Integration Test User Role",
        Description = "Integration Test User Role Description",
        Status = UserRoleStatus.Active,
        ChangedByIdentityId = KnownAuditIdentityProvider.TestFramework.IdentityId.Value,
        EicFunctions =
        {
            new UserRoleEicFunctionEntity
            {
                EicFunction = EicFunction.GridAccessProvider
            }
        },
        Permissions =
        {
            new UserRolePermissionEntity
            {
                Permission = PermissionId.UsersView,
                ChangedByIdentityId = KnownAuditIdentityProvider.TestFramework.IdentityId.Value,
            }
        }
    };

    public static EmailEventEntity ValidEmailEvent => new()
    {
        Email = new RandomlyGeneratedEmailAddress(),
        TemplateId = 1,
        TemplateParameters = "{}",
        Created = DateTimeOffset.UtcNow,
        Sent = null,
    };

    public static GridAreaEntity ValidGridArea => new()
    {
        Id = Guid.NewGuid(),
        Code = (_gridAreaCount++ % 1000).ToString(CultureInfo.InvariantCulture).PadLeft(3, '0'),
        Name = "Test Grid Area Name",
        PriceAreaCode = PriceAreaCode.Dk1,
        ChangedByIdentityId = KnownAuditIdentityProvider.TestFramework.IdentityId.Value
    };

    public static T Patch<T>(this T entity, Action<T> action)
    {
        action(entity);
        return entity;
    }
}
