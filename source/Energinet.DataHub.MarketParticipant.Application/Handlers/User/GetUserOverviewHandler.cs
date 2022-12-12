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
using Energinet.DataHub.MarketParticipant.Domain.Repositories.Slim;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.User
{
    public sealed class GetUserOverviewHandler : IRequestHandler<GetUserOverviewCommand, GetUserOverviewResponse>
    {
        private readonly IUserOverviewRepository _repository;

        public GetUserOverviewHandler(IUserOverviewRepository repository)
        {
            _repository = repository;
        }

        public async Task<GetUserOverviewResponse> Handle(GetUserOverviewCommand request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var users = await _repository.GetUsersAsync(
                request.PageNumber,
                request.PageSize,
                request.ActorId).ConfigureAwait(false);

            return new GetUserOverviewResponse(
                users.Select(x => new UserOverviewItemDto(
                    x.Id.Value,
                    x.Email.Address,
                    x.Name,
                    x.PhoneNumber,
                    x.CreatedDate,
                    x.Active)));
        }
    }
}
