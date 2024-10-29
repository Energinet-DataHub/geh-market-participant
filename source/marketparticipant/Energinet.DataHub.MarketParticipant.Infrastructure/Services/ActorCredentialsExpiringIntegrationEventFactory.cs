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
using Energinet.DataHub.MarketParticipant.Domain.Model.Events;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.Infrastructure.Model.Permissions;
using Google.Protobuf.WellKnownTypes;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Services;

// TODO: Tests
public sealed class ActorCredentialsExpiringIntegrationEventFactory : IIntegrationEventFactory<ActorCredentialsExpiring>
{
    public Task<IntegrationEvent> CreateAsync(ActorCredentialsExpiring domainEvent, int sequenceNumber)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var now = DateTime.UtcNow;
        var permission = KnownPermissions.All.Single(p => p.Id == PermissionId.ActorCredentialsManage).Claim;

        var integrationEvent = new IntegrationEvent(
            domainEvent.EventId,
            Model.Contracts.UserNotificationTriggered.EventName,
            Model.Contracts.UserNotificationTriggered.CurrentMinorVersion,
            new Model.Contracts.UserNotificationTriggered
            {
                ReasonIdentifier = "ActorCredentialsExpiring",
                TargetActorId = domainEvent.Recipient.ToString(),
                TargetPermissions = permission,
                RelatedId = domainEvent.AffectedActorId.ToString(),
                OccurredAt = now.ToTimestamp(),
                ExpiresAt = now.AddHours(23).ToTimestamp(),
            });

        return Task.FromResult(integrationEvent);
    }
}
