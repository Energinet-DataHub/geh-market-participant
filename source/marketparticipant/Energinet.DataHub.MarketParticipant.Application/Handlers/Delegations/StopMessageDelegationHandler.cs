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

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Delegations
{
    public sealed class StopMessageDelegationHandler : IRequestHandler<StopMessageDelegationCommand>
    {
        private readonly IMessageDelegationRepository _delegationRepository;
        private readonly IDomainEventRepository _domainEventRepository;
        private readonly IUnitOfWorkProvider _unitOfWorkProvider;

        public StopMessageDelegationHandler(
            IMessageDelegationRepository delegationRepository,
            IDomainEventRepository domainEventRepository,
            IUnitOfWorkProvider unitOfWorkProvider)
        {
            _delegationRepository = delegationRepository;
            _domainEventRepository = domainEventRepository;
            _unitOfWorkProvider = unitOfWorkProvider;
        }

        public async Task Handle(StopMessageDelegationCommand request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var messageDelegation = await _delegationRepository
                .GetAsync(new(request.StopMessageDelegation.Id))
                .ConfigureAwait(false);

            NotFoundValidationException.ThrowIfNull(messageDelegation, request.StopMessageDelegation.Id);

            var periodToStop = messageDelegation
                .Delegations
                .SingleOrDefault(d => d.Id.Value == request.StopMessageDelegation.PeriodId);

            NotFoundValidationException.ThrowIfNull(periodToStop, request.StopMessageDelegation.PeriodId);

            Instant? stopsAt = request.StopMessageDelegation.StopsAt.HasValue
                ? Instant.FromDateTimeOffset(request.StopMessageDelegation.StopsAt.Value)
                : null;

            var uow = await _unitOfWorkProvider
                .NewUnitOfWorkAsync()
                .ConfigureAwait(false);

            await using (uow.ConfigureAwait(false))
            {
                messageDelegation.StopDelegation(periodToStop, stopsAt);

                await _domainEventRepository
                    .EnqueueAsync(messageDelegation)
                    .ConfigureAwait(false);

                await _delegationRepository
                    .AddOrUpdateAsync(messageDelegation)
                    .ConfigureAwait(false);

                await uow.CommitAsync().ConfigureAwait(false);
            }
        }
    }
}
