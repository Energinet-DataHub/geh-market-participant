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

using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands;
using Energinet.DataHub.MarketParticipant.Domain;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers;

public sealed class SynchronizeActorsHandler : IRequestHandler<SynchronizeActorsCommand>
{
    private readonly IUnitOfWorkProvider _unitOfWorkProvider;
    private readonly IExternalActorSynchronizationService _externalActorSynchronizationService;

    public SynchronizeActorsHandler(
        IUnitOfWorkProvider unitOfWorkProvider,
        IExternalActorSynchronizationService externalActorSynchronizationService)
    {
        _unitOfWorkProvider = unitOfWorkProvider;
        _externalActorSynchronizationService = externalActorSynchronizationService;
    }

    public async Task<Unit> Handle(SynchronizeActorsCommand request, CancellationToken cancellationToken)
    {
        var uow = await _unitOfWorkProvider
            .NewUnitOfWorkAsync()
            .ConfigureAwait(false);

        await using (uow.ConfigureAwait(false))
        {
            await _externalActorSynchronizationService
                .SyncNextAsync()
                .ConfigureAwait(false);

            await uow.CommitAsync().ConfigureAwait(false);
        }

        return Unit.Value;
    }
}
