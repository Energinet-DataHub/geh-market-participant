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
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Energinet.DataHub.MarketParticipant.Authorization.Infrastructure.Persistence;

public sealed class EntityLock(IMarketParticipantDbContext context) : IEntityLock
{
    private readonly HashSet<LockableEntity> _lockedEntities = [];

    public async Task LockAsync(LockableEntity lockableEntity)
    {
        await context.CreateLockAsync(lockableEntity).ConfigureAwait(false);
        _lockedEntities.Add(lockableEntity);
    }

    public void EnsureLocked(LockableEntity lockableEntity)
    {
        ArgumentNullException.ThrowIfNull(lockableEntity);

        if (!_lockedEntities.Contains(lockableEntity))
        {
            throw new InvalidOperationException($"{lockableEntity.Name} lock is required.");
        }
    }
}
