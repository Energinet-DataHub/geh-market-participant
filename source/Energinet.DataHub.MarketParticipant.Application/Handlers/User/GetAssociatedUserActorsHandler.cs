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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.User;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.User;

public class GetAssociatedUserActorsHandler : IRequestHandler<GetAssociatedUserActorsCommand, GetAssociatedUserActorsResponse>
{
    private readonly IOrganizationRepository _repository;

    public GetAssociatedUserActorsHandler(IOrganizationRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetAssociatedUserActorsResponse> Handle(
        GetAssociatedUserActorsCommand request,
        CancellationToken cancellationToken)
    {
        var orgs = await _repository.GetAsync().ConfigureAwait(false);
        var actor = orgs
            .First(x => x.Actors.Any(y => y.ExternalActorId != null))
            .Actors
            .First(y => y.ExternalActorId != null);

        var associatedActorsExternalIds = new[] { actor.ExternalActorId!.Value };
        return new GetAssociatedUserActorsResponse(associatedActorsExternalIds);
    }
}
