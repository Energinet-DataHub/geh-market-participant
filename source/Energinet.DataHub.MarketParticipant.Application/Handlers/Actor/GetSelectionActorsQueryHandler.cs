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
using Energinet.DataHub.MarketParticipant.Application.Commands.Query.Actor;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories.Query;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Actor;

public sealed class GetSelectionActorsQueryHandler : IRequestHandler<GetSelectionActorsQueryCommand, GetSelectionActorsQueryResponse>
{
    private readonly IUserQueryRepository _userQueryRepository;
    private readonly IActorQueryRepository _actorQueryRepository;

    public GetSelectionActorsQueryHandler(
        IUserQueryRepository userQueryRepository,
        IActorQueryRepository actorQueryRepository)
    {
        _userQueryRepository = userQueryRepository;
        _actorQueryRepository = actorQueryRepository;
    }

    public async Task<GetSelectionActorsQueryResponse> Handle(
        GetSelectionActorsQueryCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var actorIds = await _userQueryRepository
            .GetActorsAsync(new ExternalUserId(request.UserId))
            .ConfigureAwait(false);

        var actors = await _actorQueryRepository
            .GetSelectionActorsAsync(actorIds)
            .ConfigureAwait(false);

        return new GetSelectionActorsQueryResponse(
            actors.Select(x => new SelectionActorDto(x.Id, x.Gln, x.Name)));
    }
}
