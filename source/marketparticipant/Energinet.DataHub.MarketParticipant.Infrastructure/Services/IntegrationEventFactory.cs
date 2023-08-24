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
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Events;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using NodaTime.Serialization.Protobuf;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Services;

public sealed class IntegrationEventFactory : IIntegrationEventFactory
{
    private readonly IGridAreaRepository _gridAreaRepository;

    public IntegrationEventFactory(IGridAreaRepository gridAreaRepository)
    {
        _gridAreaRepository = gridAreaRepository;
    }

    public Task<IntegrationEvent> CreateAsync(DomainEvent domainEvent)
    {
        return CreateEventAsync((dynamic)domainEvent);
    }

    private static Task<IntegrationEvent> CreateEventAsync(ActorActivated domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var integrationEvent = new IntegrationEvent(
            domainEvent.EventId,
            Model.Contracts.ActorActivated.EventName,
            Model.Contracts.ActorActivated.CurrentMinorVersion,
            new Model.Contracts.ActorActivated
            {
                ActorNumber = domainEvent.ActorNumber.Value,
                ActorNumberType = domainEvent.ActorNumber.Type switch
                {
                    ActorNumberType.Eic => Model.Contracts.ActorNumberType.Eic,
                    ActorNumberType.Gln => Model.Contracts.ActorNumberType.Gln,
                    _ => throw new NotSupportedException($"Actor number type {domainEvent.ActorNumber.Type} is not supported in integration event.")
                },
                ExternalActorId = domainEvent.ExternalActorId.ToString(),
                ValidFrom = domainEvent.ValidFrom.ToTimestamp()
            });

        return Task.FromResult(integrationEvent);
    }

    private async Task<IntegrationEvent> CreateEventAsync(GridAreaOwnershipAssigned domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var gridArea = await _gridAreaRepository
            .GetAsync(domainEvent.GridAreaId)
            .ConfigureAwait(false);

        NotFoundValidationException.ThrowIfNull(gridArea, domainEvent.GridAreaId.Value);

        return new IntegrationEvent(
            domainEvent.EventId,
            Model.Contracts.GridAreaOwnershipAssigned.EventName,
            Model.Contracts.GridAreaOwnershipAssigned.CurrentMinorVersion,
            new Model.Contracts.GridAreaOwnershipAssigned
            {
                ActorNumber = domainEvent.ActorNumber.Value,
                ActorRole = domainEvent.ActorRole switch
                {
                    EicFunction.GridAccessProvider => Model.Contracts.EicFunction.GridAccessProvider,
                    _ => throw new NotSupportedException($"Actor role {domainEvent.ActorRole} is not supported in integration event.")
                },
                GridAreaCode = gridArea.Code.Value,
                ValidFrom = domainEvent.ValidFrom.ToTimestamp()
            });
    }
}
