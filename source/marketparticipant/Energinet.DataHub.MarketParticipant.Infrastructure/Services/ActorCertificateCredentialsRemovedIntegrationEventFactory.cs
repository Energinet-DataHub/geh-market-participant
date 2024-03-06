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
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Events;
using NodaTime.Serialization.Protobuf;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Services;

public sealed class ActorCertificateCredentialsRemovedIntegrationEventFactory : IIntegrationEventFactory<ActorCertificateCredentialsRemoved>
{
    public Task<IntegrationEvent> CreateAsync(ActorCertificateCredentialsRemoved domainEvent, int sequenceNumber)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var integrationEvent = new IntegrationEvent(
            domainEvent.EventId,
            Model.Contracts.ActorCertificateCredentialsRemoved.EventName,
            Model.Contracts.ActorCertificateCredentialsRemoved.CurrentMinorVersion,
            new Model.Contracts.ActorCertificateCredentialsRemoved
            {
                ActorNumber = domainEvent.ActorNumber.Value,
                ActorRole = domainEvent.ActorRole switch
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
                    _ => throw new NotSupportedException($"Actor role {domainEvent.ActorRole} is not supported in integration event.")
                },
                CertificateThumbprint = domainEvent.CertificateThumbprint,
                ValidFrom = domainEvent.ValidFrom.ToTimestamp(),
                SequenceNumber = sequenceNumber
            });

        return Task.FromResult(integrationEvent);
    }
}
