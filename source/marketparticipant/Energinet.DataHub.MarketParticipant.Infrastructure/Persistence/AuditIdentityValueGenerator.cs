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
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence;

public sealed class AuditIdentityValueGenerator : ValueGenerator
{
    public override bool GeneratesTemporaryValues => false;

    protected override object NextValue(EntityEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var dbContext = (MarketParticipantDbContext)entry.Context;
        return dbContext.AuditIdentityProvider.IdentityId.Value;
    }
}
