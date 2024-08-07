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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;

namespace Energinet.DataHub.MarketParticipant.Domain.Services;

public sealed class ExternalActorIdConfigurationService : IExternalActorIdConfigurationService
{
    private readonly IActiveDirectoryB2CService _activeDirectoryService;

    public ExternalActorIdConfigurationService(IActiveDirectoryB2CService activeDirectoryService)
    {
        _activeDirectoryService = activeDirectoryService;
    }

    public async Task AssignExternalActorIdAsync(Actor actor)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (actor.ExternalActorId != null)
        {
            if (actor.Status is not (ActorStatus.Active or ActorStatus.Passive))
            {
                // There is an external id, but it is no longer allowed.
                await _activeDirectoryService
                    .DeleteAppRegistrationAsync(actor)
                    .ConfigureAwait(false);
            }
        }
        else
        {
            if (actor.Status is ActorStatus.Active or ActorStatus.Passive)
            {
                await _activeDirectoryService
                    .AssignApplicationRegistrationAsync(actor)
                    .ConfigureAwait(false);
            }
        }
    }
}
