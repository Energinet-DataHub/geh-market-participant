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
using Energinet.DataHub.MarketParticipant.Integration.Model.Dtos;
using Energinet.DataHub.MarketParticipant.Integration.Model.Exceptions;

namespace Energinet.DataHub.MarketParticipant.Integration.Model.Parsers
{
    public class SharedIntegrationEventParser : ISharedIntegrationEventParser
    {
        public BaseIntegrationEvent Parse(byte[] protoContract)
        {
            if (TryParseActorUpdatedIntegrationEvent(protoContract, out var actorUpdatedEvent))
            {
                return actorUpdatedEvent;
            }

            if (TryParseOrganizationUpdatedIntegrationEvent(protoContract, out var organizationUpdatedIntegrationEvent))
            {
                return organizationUpdatedIntegrationEvent;
            }

            if (TryParseGridAreaUpdatedIntegrationEvent(protoContract, out var gridAreaUpdatedEvent))
            {
                return gridAreaUpdatedEvent;
            }

            throw new MarketParticipantException("IntegrationEventParser not found");
        }

        private static bool TryParseActorUpdatedIntegrationEvent(
            byte[] protoContract,
            out ActorUpdatedIntegrationEvent actorUpdatedEvent)
        {
            try
            {
                var actorUpdatedEventParser = new ActorUpdatedIntegrationEventParser();
                var actorEvent = actorUpdatedEventParser.Parse(protoContract);
                actorUpdatedEvent = actorEvent;
                return true;
            }
#pragma warning disable CA1031
            catch (Exception)
#pragma warning restore CA1031
            {
                actorUpdatedEvent = null!;
                return false;
            }
        }

        private static bool TryParseGridAreaUpdatedIntegrationEvent(
            byte[] protoContract,
            out GridAreaCreatedIntegrationEvent gridAreaCreatedIntegrationEvent)
        {
            try
            {
                var gridAreaUpdatedEventParser = new GridAreaIntegrationEventParser();
                var gridAreaUpdatedEvent = gridAreaUpdatedEventParser.Parse(protoContract);
                gridAreaCreatedIntegrationEvent = gridAreaUpdatedEvent;
                return true;
            }
#pragma warning disable CA1031
            catch (Exception)
#pragma warning restore CA1031
            {
                gridAreaCreatedIntegrationEvent = null!;
                return false;
            }
        }

        private static bool TryParseOrganizationUpdatedIntegrationEvent(
            byte[] protoContract,
            out OrganizationUpdatedIntegrationEvent organizationUpdatedIntegrationEvent)
        {
            try
            {
                var organizationUpdatedIntegrationEventParser = new OrganizationUpdatedIntegrationEventParser();
                organizationUpdatedIntegrationEvent = organizationUpdatedIntegrationEventParser.Parse(protoContract);
                return true;
            }
#pragma warning disable CA1031
            catch (Exception)
#pragma warning restore CA1031
            {
                organizationUpdatedIntegrationEvent = null!;
                return false;
            }
        }
    }
}
