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
using System.Threading.Tasks;
using Energinet.DataHub.Core.Messaging.Communication;
using Energinet.DataHub.MarketParticipant.Domain.Model.Delegations;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Model.Contracts;
using NodaTime.Serialization.Protobuf;
using EicFunction = Energinet.DataHub.MarketParticipant.Domain.Model.EicFunction;
using MessageDelegationConfigured = Energinet.DataHub.MarketParticipant.Domain.Model.Events.MessageDelegationConfigured;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Services;

public sealed class MessageDelegationConfiguredIntegrationEventFactory : IIntegrationEventFactory<MessageDelegationConfigured>
{
    private readonly IActorRepository _actorRepository;
    private readonly IGridAreaRepository _gridAreaRepository;

    public MessageDelegationConfiguredIntegrationEventFactory(
        IActorRepository actorRepository,
        IGridAreaRepository gridAreaRepository)
    {
        _actorRepository = actorRepository;
        _gridAreaRepository = gridAreaRepository;
    }

    public async Task<IntegrationEvent> CreateAsync(MessageDelegationConfigured domainEvent, int sequenceNumber)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var delegatedBy = await _actorRepository
            .GetAsync(domainEvent.DelegatedBy)
            .ConfigureAwait(false);

        var delegatedByActorNumber = delegatedBy!.ActorNumber.Value;
        var delegatedByMarketRole = delegatedBy.MarketRoles.Single().Function;

        var delegatedTo = await _actorRepository
            .GetAsync(domainEvent.DelegatedTo)
            .ConfigureAwait(false);

        var delegatedToActorNumber = delegatedTo!.ActorNumber.Value;
        var delegatedToMarketRole = delegatedTo.MarketRoles.Single().Function;

        var gridArea = await _gridAreaRepository
            .GetAsync(domainEvent.GridAreaId)
            .ConfigureAwait(false);

        var integrationEvent = new IntegrationEvent(
            domainEvent.EventId,
            Model.Contracts.MessageDelegationConfigured.EventName,
            Model.Contracts.MessageDelegationConfigured.CurrentMinorVersion,
            new Model.Contracts.MessageDelegationConfigured
            {
                DelegatedByActorNumber = delegatedByActorNumber,
                DelegatedByActorRole = MapMarketRole(delegatedByMarketRole),
                DelegatedToActorNumber = delegatedToActorNumber,
                DelegatedToActorRole = MapMarketRole(delegatedToMarketRole),
                GridAreaCode = gridArea!.Code.Value,
                MessageType = domainEvent.MessageType switch
                {
                    DelegationMessageType.Rsm012Inbound => DelegatedMessageType.MessageTypeRsm012Inbound,
                    DelegationMessageType.Rsm012Outbound => DelegatedMessageType.MessageTypeRsm012Outbound,
                    DelegationMessageType.Rsm014Inbound => DelegatedMessageType.MessageTypeRsm014Inbound,
                    DelegationMessageType.Rsm016Inbound => DelegatedMessageType.MessageTypeRsm016Inbound,
                    DelegationMessageType.Rsm016Outbound => DelegatedMessageType.MessageTypeRsm016Outbound,
                    DelegationMessageType.Rsm017Inbound => DelegatedMessageType.MessageTypeRsm017Inbound,
                    DelegationMessageType.Rsm017Outbound => DelegatedMessageType.MessageTypeRsm017Outbound,
                    DelegationMessageType.Rsm018Inbound => DelegatedMessageType.MessageTypeRsm018Inbound,
                    DelegationMessageType.Rsm019Inbound => DelegatedMessageType.MessageTypeRsm019Inbound,
                    _ => throw new InvalidOperationException($"Delegation message type {domainEvent.MessageType} is not supported in integration event.")
                },
                StartsAt = domainEvent.StartsAt.ToTimestamp(),
                StopsAt = domainEvent.StopsAt.ToTimestamp(),
                SequenceNumber = sequenceNumber
            });

        return integrationEvent;
    }

    private static Model.Contracts.EicFunction MapMarketRole(EicFunction eicFunction)
    {
        return eicFunction switch
        {
            EicFunction.GridAccessProvider => Model.Contracts.EicFunction.GridAccessProvider,
            EicFunction.BalanceResponsibleParty => Model.Contracts.EicFunction.BalanceResponsibleParty,
            EicFunction.BillingAgent => Model.Contracts.EicFunction.BillingAgent,
            EicFunction.EnergySupplier => Model.Contracts.EicFunction.EnergySupplier,
            EicFunction.ImbalanceSettlementResponsible => Model.Contracts.EicFunction.ImbalanceSettlementResponsible,
            EicFunction.MeteredDataAdministrator => Model.Contracts.EicFunction.MeteredDataAdministrator,
            EicFunction.MeteredDataResponsible => Model.Contracts.EicFunction.MeteredDataResponsible,
            EicFunction.MeteringPointAdministrator => Model.Contracts.EicFunction.MeteringPointAdministrator,
            EicFunction.SystemOperator => Model.Contracts.EicFunction.SystemOperator,
            EicFunction.DanishEnergyAgency => Model.Contracts.EicFunction.DanishEnergyAgency,
            EicFunction.DataHubAdministrator => Model.Contracts.EicFunction.DatahubAdministrator,
            EicFunction.IndependentAggregator => Model.Contracts.EicFunction.IndependentAggregator,
            EicFunction.SerialEnergyTrader => Model.Contracts.EicFunction.SerialEnergyTrader,
            EicFunction.MeterOperator => Model.Contracts.EicFunction.MeterOperator,
            EicFunction.Delegated => Model.Contracts.EicFunction.Delegated,
            _ => throw new NotSupportedException($"Market role {eicFunction} is not supported in integration event.")
        };
    }
}
