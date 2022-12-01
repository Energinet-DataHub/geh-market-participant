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
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Energinet.DataHub.Core.App.Common.Abstractions.Users;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories.Slim;

namespace Energinet.DataHub.MarketParticipant.Application.Security;

public sealed class FrontendUserProvider : IUserProvider<FrontendUser>
{
    private readonly IActorRepository _actorRepository;

    public FrontendUserProvider(IActorRepository actorRepository)
    {
        _actorRepository = actorRepository;
    }

    public async Task<FrontendUser?> ProvideUserAsync(
        Guid userId,
        Guid actorId,
        bool isFas,
        IEnumerable<Claim> claims)
    {
        var actor = await _actorRepository.GetActorAsync(actorId).ConfigureAwait(false);
        return actor is { Status: ActorStatus.Active or ActorStatus.Passive }
            ? new FrontendUser(
                userId,
                actor.OrganizationId,
                actor.ActorId,
                isFas)
            : null;
    }
}
