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
using Energinet.DataHub.MarketParticipant.Application.Commands.Actor;
using Energinet.DataHub.MarketParticipant.Application.Mappers;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Actor;

public sealed class GetAllActorsHandler : IRequestHandler<GetAllActorsCommand, GetActorsResponse>
{
    private readonly IActorRepository _actorRepository;

    public GetAllActorsHandler(IActorRepository actorRepository)
    {
        _actorRepository = actorRepository;
    }

    public async Task<GetActorsResponse> Handle(
        GetAllActorsCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var actors = await _actorRepository
            .GetActorsAsync()
            .ConfigureAwait(false);

        return new GetActorsResponse(actors.Select(OrganizationMapper.Map));
    }
}
