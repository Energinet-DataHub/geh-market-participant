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
using Energinet.DataHub.MarketParticipant.Authorization.Infrastructure.Persistence.Model;

namespace Energinet.DataHub.MarketParticipant.Authorization.Infrastructure.Persistence.Audit;

public sealed class KnownAuditIdentityProvider : IAuditIdentityProvider
{
    private KnownAuditIdentityProvider(string friendlyName, string identityId)
    {
        FriendlyName = friendlyName;
        IdentityId = new AuditIdentity(Guid.Parse(identityId));
    }

    public static KnownAuditIdentityProvider Migration { get; } = new("Datamigrering", "00000000-FFFF-FFFF-FFFF-000000000000");
    public static KnownAuditIdentityProvider TestFramework { get; } = new("Test Framework", "AAAAAAAA-BBBB-CCCC-DDDD-000000000000");
    public static KnownAuditIdentityProvider OrganizationBackgroundService { get; } = new("Aktørsynkronisering", "00000000-1111-0000-0001-000000000000");
    public static KnownAuditIdentityProvider AuthApiBackgroundService { get; } = new("Auth API", "00000000-1111-0000-0002-000000000000");
    public static KnownAuditIdentityProvider ProcessManagerBackgroundJobs { get; } = new("Baggrundsjob", "C861C5E2-8DDA-43E5-A5D0-B94834EE3FF6");

    public AuditIdentity IdentityId { get; }
    public string FriendlyName { get; }
}
