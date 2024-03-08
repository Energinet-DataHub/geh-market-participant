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
using Energinet.DataHub.MarketParticipant.Application.Commands.Delegations;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;
using NodaTime.Extensions;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Delegations
{
    public sealed class UpdateDelegationForActorHandler : IRequestHandler<UpdateActorDelegationCommand, UpdateActorDelegationResponse>
    {
        private readonly IActorDelegationRepository _actorDelegationRepository;

        public UpdateDelegationForActorHandler(
            IActorDelegationRepository actorDelegationRepository)
        {
            _actorDelegationRepository = actorDelegationRepository;
        }

        public async Task<UpdateActorDelegationResponse> Handle(UpdateActorDelegationCommand request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));

            var actorDelegation = await _actorDelegationRepository
                .GetAsync(request.UpdateActorDelegation.Id)
                .ConfigureAwait(false);

            if (actorDelegation is null) return new UpdateActorDelegationResponse("NotFound");
            actorDelegation.SetExpiresAt(request.UpdateActorDelegation.ExpiresAt.ToInstant());

            await _actorDelegationRepository.AddOrUpdateAsync(actorDelegation).ConfigureAwait(false);

            var responseMessage = request.UpdateActorDelegation.ExpiresAt > DateTimeOffset.UtcNow ? "Updated" : "Stopped";
            return new UpdateActorDelegationResponse(responseMessage);
        }
    }
}
