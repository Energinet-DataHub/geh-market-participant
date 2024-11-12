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
using System.Threading.Tasks;
using Energinet.DataHub.Core.Messaging.Communication;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Extensions;
using NodaTime.Serialization.Protobuf;
using DelegatedProcess = Energinet.DataHub.MarketParticipant.Domain.Model.Delegations.DelegatedProcess;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Services;

public sealed class ProcessDelegationConfiguredIntegrationEventFactory : IIntegrationEventFactory<Domain.Model.Events.ProcessDelegationConfigured>
{
    private readonly IActorRepository _actorRepository;
    private readonly IGridAreaRepository _gridAreaRepository;

    public ProcessDelegationConfiguredIntegrationEventFactory(
        IActorRepository actorRepository,
        IGridAreaRepository gridAreaRepository)
    {
        _actorRepository = actorRepository;
        _gridAreaRepository = gridAreaRepository;
    }

    public async Task<IntegrationEvent> CreateAsync(Domain.Model.Events.ProcessDelegationConfigured domainEvent, int sequenceNumber)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var delegatedBy = await _actorRepository
            .GetAsync(domainEvent.DelegatedBy)
            .ConfigureAwait(false);

        var delegatedByActorNumber = delegatedBy!.ActorNumber.Value;
        var delegatedByMarketRole = delegatedBy.MarketRole.Function;

        var delegatedTo = await _actorRepository
            .GetAsync(domainEvent.DelegatedTo)
            .ConfigureAwait(false);

        var delegatedToActorNumber = delegatedTo!.ActorNumber.Value;
        var delegatedToMarketRole = delegatedTo.MarketRole.Function;

        var gridArea = await _gridAreaRepository
            .GetAsync(domainEvent.GridAreaId)
            .ConfigureAwait(false);

        var integrationEvent = new IntegrationEvent(
            domainEvent.EventId,
            Model.Contracts.ProcessDelegationConfigured.EventName,
            Model.Contracts.ProcessDelegationConfigured.CurrentMinorVersion,
            new Model.Contracts.ProcessDelegationConfigured
            {
                DelegatedByActorNumber = delegatedByActorNumber,
                DelegatedByActorRole = delegatedByMarketRole.MapToContract(),
                DelegatedToActorNumber = delegatedToActorNumber,
                DelegatedToActorRole = delegatedToMarketRole.MapToContract(),
                GridAreaCode = gridArea!.Code.Value,
                Process = domainEvent.Process switch
                {
                    DelegatedProcess.RequestEnergyResults => Model.Contracts.DelegatedProcess.ProcessRequestEnergyResults,
                    DelegatedProcess.ReceiveEnergyResults => Model.Contracts.DelegatedProcess.ProcessReceiveEnergyResults,
                    DelegatedProcess.RequestWholesaleResults => Model.Contracts.DelegatedProcess.ProcessRequestWholesaleResults,
                    DelegatedProcess.ReceiveWholesaleResults => Model.Contracts.DelegatedProcess.ProcessReceiveWholesaleResults,
                    DelegatedProcess.RequestMeteringPointData => Model.Contracts.DelegatedProcess.ProcessRequestMeteringpointData,
                    DelegatedProcess.ReceiveMeteringPointData => Model.Contracts.DelegatedProcess.ProcessReceiveMeteringpointData,
                    _ => throw new InvalidOperationException($"Delegation process type {domainEvent.Process} is not supported in integration event.")
                },
                StartsAt = domainEvent.StartsAt.ToTimestamp(),
                StopsAt = domainEvent.StopsAt.ToTimestamp(),
                SequenceNumber = sequenceNumber
            });

        return integrationEvent;
    }
}
