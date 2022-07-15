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
using Energinet.DataHub.MarketParticipant.Integration.Model.Parsers.Actor;
using Energinet.DataHub.MarketParticipant.Integration.Model.Parsers.GridArea;
using Energinet.DataHub.MarketParticipant.Integration.Model.Parsers.Organization;

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

            if (TryParseActorStatusChangedIntegrationEvent(protoContract, out var actorStatusChangedEvent))
            {
                return actorStatusChangedEvent;
            }

            if (TryParseGridAreaUpdatedIntegrationEvent(protoContract, out var gridAreaUpdatedEvent))
            {
                return gridAreaUpdatedEvent;
            }

            if (TryParseGridAreaCreatedIntegrationEvent(protoContract, out var gridAreaCreatedEvent))
            {
                return gridAreaCreatedEvent;
            }

            if (TryParseGridAreaNameChangedIntegrationEvent(protoContract, out var gridAreaNameChangedEvent))
            {
                return gridAreaNameChangedEvent;
            }

            if (TryParseOrganizationCreatedIntegrationEvent(protoContract, out var organizationCreatedIntegrationEvent))
            {
                return organizationCreatedIntegrationEvent;
            }

            if (TryParseOrganizationUpdatedIntegrationEvent(protoContract, out var organizationUpdatedIntegrationEvent))
            {
                return organizationUpdatedIntegrationEvent;
            }

            if (TryParseOrganizationNameChangedIntegrationEvent(protoContract, out var organizationNameChangedIntegrationEvent))
            {
                return organizationNameChangedIntegrationEvent;
            }

            if (TryParseOrganizationBusinessRegisterIdentifierIntegrationEvent(protoContract, out var organizationBusinessRegisterIdentifierIntegrationEvent))
            {
                return organizationBusinessRegisterIdentifierIntegrationEvent;
            }

            if (TryParseOrganizationCommentChangedIntegrationEvent(protoContract, out var organizationCommentChangedIntegrationEvent))
            {
                return organizationCommentChangedIntegrationEvent;
            }

            if (TryParseOrganizationAddressChangedIntegrationEvent(protoContract, out var organizationAddressChangedIntegrationEvent))
            {
                return organizationAddressChangedIntegrationEvent;
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

        private static bool TryParseActorStatusChangedIntegrationEvent(
            byte[] protoContract,
            out ActorStatusChangedIntegrationEvent actorStatusChangedEvent)
        {
            try
            {
                var actorStatusChangedEventParser = new ActorStatusChangedIntegrationEventParser();
                var actorEvent = actorStatusChangedEventParser.Parse(protoContract);
                actorStatusChangedEvent = actorEvent;
                return true;
            }
#pragma warning disable CA1031
            catch (Exception)
#pragma warning restore CA1031
            {
                actorStatusChangedEvent = null!;
                return false;
            }
        }

        private static bool TryParseGridAreaUpdatedIntegrationEvent(
            byte[] protoContract,
            out GridAreaUpdatedIntegrationEvent gridAreaUpdatedIntegrationEvent)
        {
            try
            {
                var gridAreaUpdatedEventParser = new GridAreaUpdatedIntegrationEventParser();
                var gridAreaUpdatedEvent = gridAreaUpdatedEventParser.Parse(protoContract);
                gridAreaUpdatedIntegrationEvent = gridAreaUpdatedEvent;
                return true;
            }
#pragma warning disable CA1031
            catch (Exception)
#pragma warning restore CA1031
            {
                gridAreaUpdatedIntegrationEvent = null!;
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

        private static bool TryParseGridAreaCreatedIntegrationEvent(
            byte[] protoContract,
            out GridAreaCreatedIntegrationEvent gridAreaCreatedIntegrationEvent)
        {
            try
            {
                var gridAreaCreatedEventParser = new GridAreaIntegrationEventParser();
                var gridAreaCreatedEvent = gridAreaCreatedEventParser.Parse(protoContract);
                gridAreaCreatedIntegrationEvent = gridAreaCreatedEvent;
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

        private static bool TryParseGridAreaNameChangedIntegrationEvent(
            byte[] protoContract,
            out GridAreaNameChangedIntegrationEvent gridAreaNameChangedIntegrationEvent)
        {
            try
            {
                var gridAreaNameChangedEventParser = new GridAreaNameChangedIntegrationEventParser();
                var gridAreaNameChangedEvent = gridAreaNameChangedEventParser.Parse(protoContract);
                gridAreaNameChangedIntegrationEvent = gridAreaNameChangedEvent;
                return true;
            }
#pragma warning disable CA1031
            catch (Exception)
#pragma warning restore CA1031
            {
                gridAreaNameChangedIntegrationEvent = null!;
                return false;
            }
        }

        private static bool TryParseOrganizationCreatedIntegrationEvent(
            byte[] protoContract,
            out OrganizationCreatedIntegrationEvent organizationCreatedIntegrationEvent)
        {
            try
            {
                var organizationCreatedIntegrationEventParser = new OrganizationCreatedIntegrationEventParser();
                organizationCreatedIntegrationEvent = organizationCreatedIntegrationEventParser.Parse(protoContract);
                return true;
            }
#pragma warning disable CA1031
            catch (Exception)
#pragma warning restore CA1031
            {
                organizationCreatedIntegrationEvent = null!;
                return false;
            }
        }

        private static bool TryParseOrganizationNameChangedIntegrationEvent(
            byte[] protoContract,
            out OrganizationNameChangedIntegrationEvent organizationNameChangedIntegrationEvent)
        {
            try
            {
                var organizationNameChangedIntegrationEventParser = new OrganizationNameChangedIntegrationEventParser();
                organizationNameChangedIntegrationEvent = organizationNameChangedIntegrationEventParser.Parse(protoContract);
                return true;
            }
#pragma warning disable CA1031
            catch (Exception)
#pragma warning restore CA1031
            {
                organizationNameChangedIntegrationEvent = null!;
                return false;
            }
        }

        private static bool TryParseOrganizationCommentChangedIntegrationEvent(
            byte[] protoContract,
            out OrganizationCommentChangedIntegrationEvent organizationCommentChangedIntegrationEvent)
        {
            try
            {
                var organizationCommentChangedIntegrationEventParser = new OrganizationCommentChangedIntegrationEventParser();
                organizationCommentChangedIntegrationEvent = organizationCommentChangedIntegrationEventParser.Parse(protoContract);
                return true;
            }
#pragma warning disable CA1031
            catch (Exception)
#pragma warning restore CA1031
            {
                organizationCommentChangedIntegrationEvent = null!;
                return false;
            }
        }

        private static bool TryParseOrganizationAddressChangedIntegrationEvent(
            byte[] protoContract,
            out OrganizationAddressChangedIntegrationEvent organizationAddressChangedIntegrationEvent)
        {
            try
            {
                var organizationAddressChangedIntegrationEventParser = new OrganizationAddressChangedIntegrationEventParser();
                organizationAddressChangedIntegrationEvent = organizationAddressChangedIntegrationEventParser.Parse(protoContract);
                return true;
            }
#pragma warning disable CA1031
            catch (Exception)
#pragma warning restore CA1031
            {
                organizationAddressChangedIntegrationEvent = null!;
                return false;
            }
        }

        private static bool TryParseOrganizationBusinessRegisterIdentifierIntegrationEvent(
            byte[] protoContract,
            out OrganizationBusinessRegisterIdentifierChangedIntegrationEvent organizationBusinessRegisterIdentifierIntegrationEvent)
        {
            try
            {
                var organizationBusinessRegisterIdentifierIntegrationEventParser = new OrganizationBusinessRegisterIdentifierChangedIntegrationEventParser();
                organizationBusinessRegisterIdentifierIntegrationEvent = organizationBusinessRegisterIdentifierIntegrationEventParser.Parse(protoContract);
                return true;
            }
#pragma warning disable CA1031
            catch (Exception)
#pragma warning restore CA1031
            {
                organizationBusinessRegisterIdentifierIntegrationEvent = null!;
                return false;
            }
        }
    }
}
