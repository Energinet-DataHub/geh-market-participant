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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.User;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories.Slim;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.User;

public sealed class GetAssociatedUserActorsHandler : IRequestHandler<GetAssociatedUserActorsCommand, GetAssociatedUserActorsResponse>
{
    private readonly IUserRepository _userRepository;

    public GetAssociatedUserActorsHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<GetAssociatedUserActorsResponse> Handle(
        GetAssociatedUserActorsCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var actorIds = await _userRepository
            .GetActorsAsync(new ExternalUserId(request.ExternalUserId))
            .ConfigureAwait(false);

        return new GetAssociatedUserActorsResponse(actorIds);
    }
}
