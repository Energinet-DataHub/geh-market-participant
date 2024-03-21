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
using Energinet.DataHub.MarketParticipant.Application.Commands.Delegations;
using Energinet.DataHub.MarketParticipant.Domain;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;
using NodaTime;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Delegations;

public sealed class StopProcessDelegationHandler : IRequestHandler<StopProcessDelegationCommand>
{
    private readonly IProcessDelegationRepository _delegationRepository;
    private readonly IDomainEventRepository _domainEventRepository;
    private readonly IUnitOfWorkProvider _unitOfWorkProvider;

    public StopProcessDelegationHandler(
        IProcessDelegationRepository delegationRepository,
        IDomainEventRepository domainEventRepository,
        IUnitOfWorkProvider unitOfWorkProvider)
    {
        _delegationRepository = delegationRepository;
        _domainEventRepository = domainEventRepository;
        _unitOfWorkProvider = unitOfWorkProvider;
    }

    public async Task Handle(StopProcessDelegationCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var processDelegation = await _delegationRepository
            .GetAsync(new(request.StopProcessDelegation.Id))
            .ConfigureAwait(false);

        NotFoundValidationException.ThrowIfNull(processDelegation, request.StopProcessDelegation.Id);

        var periodToStop = processDelegation
            .Delegations
            .SingleOrDefault(d => d.Id.Value == request.StopProcessDelegation.PeriodId);

        NotFoundValidationException.ThrowIfNull(periodToStop, request.StopProcessDelegation.PeriodId);

        Instant? stopsAt = request.StopProcessDelegation.StopsAt.HasValue
            ? Instant.FromDateTimeOffset(request.StopProcessDelegation.StopsAt.Value)
            : null;

        var uow = await _unitOfWorkProvider
            .NewUnitOfWorkAsync()
            .ConfigureAwait(false);

        await using (uow.ConfigureAwait(false))
        {
            processDelegation.StopDelegation(periodToStop, stopsAt);

            await _domainEventRepository
                .EnqueueAsync(processDelegation)
                .ConfigureAwait(false);

            await _delegationRepository
                .AddOrUpdateAsync(processDelegation)
                .ConfigureAwait(false);

            await uow.CommitAsync().ConfigureAwait(false);
        }
    }
}
