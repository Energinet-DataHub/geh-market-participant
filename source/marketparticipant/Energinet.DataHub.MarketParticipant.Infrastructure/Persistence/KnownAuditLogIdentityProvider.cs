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

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence;

public sealed class KnownAuditLogIdentityProvider : IAuditLogIdentityProvider
{
    public KnownAuditLogIdentityProvider(Guid identityId)
    {
        IdentityId = identityId;
    }

    public static KnownAuditLogIdentityProvider TestFramework { get; } = new(Guid.Parse("AAAAAAAA-BBBB-CCCC-DDDD-000000000000"));
    public static KnownAuditLogIdentityProvider OrganizationBackgroundService { get; } = new(Guid.Parse("00000000-1111-0000-0001-000000000000"));

    public Guid IdentityId { get; }
}
