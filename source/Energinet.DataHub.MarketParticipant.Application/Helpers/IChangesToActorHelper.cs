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
using System.Collections.Generic;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Actor;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.IntegrationEvents;
namespace Energinet.DataHub.MarketParticipant.Application.Helpers;

/// <summary>
/// Finds changes made to an actor
/// </summary>
public interface IChangesToActorHelper
{
    /// <summary>
    /// Find any possible changes made to an Actor
    /// </summary>
    /// <param name="organizationId"></param>
    /// <param name="existingActor"></param>
    /// <param name="incomingActor"></param>
    /// <returns>A list of integration events to send to a message queue</returns>
    Task<List<IIntegrationEvent>> FindChangesMadeToActorAsync(
        OrganizationId organizationId,
        Actor existingActor,
        UpdateActorCommand incomingActor);

    /// <summary>
    /// Returns integration event for changed ExternalActorId, if any
    /// </summary>
    /// <param name="newActorState">Actor with updated state</param>
    /// <param name="organizationId">Organization Id</param>
    /// <param name="previousExternalId">Previous External Actor Id</param>
    /// <param name="integrationEvents">List with integration events to add to</param>
    /// <returns>Integration event for changed external actor id</returns>
    void SetIntegrationEventForExternalActorId(
        Actor newActorState,
        OrganizationId organizationId,
        Guid? previousExternalId,
#pragma warning disable CA1002
        List<IIntegrationEvent> integrationEvents);
#pragma warning restore CA1002
}
