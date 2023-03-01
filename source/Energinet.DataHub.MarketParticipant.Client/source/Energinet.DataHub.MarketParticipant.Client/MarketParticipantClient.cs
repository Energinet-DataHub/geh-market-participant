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
using Energinet.DataHub.MarketParticipant.Client.Models;

namespace Energinet.DataHub.MarketParticipant.Client
{
    public sealed class MarketParticipantClient : IMarketParticipantClient
    {
        private readonly IMarketParticipantOrganizationClient _marketParticipantOrganizationClient;
        private readonly IMarketParticipantUserClient _marketParticipantUserClient;
        private readonly IMarketParticipantActorClient _marketParticipantActorClient;
        private readonly IMarketParticipantGridAreaClient _marketParticipantGridAreaClient;
        private readonly IMarketParticipantActorContactClient _marketParticipantActorContactClient;
        private readonly IMarketParticipantGridAreaOverviewClient _marketParticipantGridAreaOverviewClient;
        private readonly ITokenClient _tokenClient;
        private readonly IMarketParticipantUserOverviewClient _marketParticipantUserOverviewClient;
        private readonly IMarketParticipantActorQueryClient _marketParticipantActorQueryClient;

        public MarketParticipantClient(IMarketParticipantClientFactory clientFactory)
        {
            _marketParticipantOrganizationClient = new MarketParticipantOrganizationClient(clientFactory);
            _marketParticipantUserClient = new MarketParticipantUserClient(clientFactory);
            _marketParticipantActorClient = new MarketParticipantActorClient(clientFactory);
            _marketParticipantGridAreaClient = new MarketParticipantGridAreaClient(clientFactory);
            _marketParticipantActorContactClient = new MarketParticipantActorContactClient(clientFactory);
            _marketParticipantGridAreaOverviewClient = new MarketParticipantGridAreaOverviewClient(clientFactory);
            _tokenClient = new TokenClient(clientFactory);
            _marketParticipantUserOverviewClient = new MarketParticipantUserOverviewClient(clientFactory);
            _marketParticipantActorQueryClient = new MarketParticipantActorQueryClient(clientFactory);
        }

        public Task<IEnumerable<OrganizationDto>> GetOrganizationsAsync()
        {
            return _marketParticipantOrganizationClient.GetOrganizationsAsync();
        }

        public Task<OrganizationDto> GetOrganizationAsync(Guid organizationId)
        {
            return _marketParticipantOrganizationClient.GetOrganizationAsync(organizationId);
        }

        public Task<Guid> CreateOrganizationAsync(CreateOrganizationDto organizationDto)
        {
            return _marketParticipantOrganizationClient.CreateOrganizationAsync(organizationDto);
        }

        public Task UpdateOrganizationAsync(Guid organizationId, ChangeOrganizationDto organizationDto)
        {
            return _marketParticipantOrganizationClient.UpdateOrganizationAsync(organizationId, organizationDto);
        }

        public Task<IEnumerable<ActorDto>> GetActorsAsync(Guid organizationId)
        {
            return _marketParticipantOrganizationClient.GetActorsAsync(organizationId);
        }

        public Task<ActorDto> GetActorAsync(Guid actorId)
        {
            return _marketParticipantActorClient.GetActorAsync(actorId);
        }

        public Task<Guid> CreateActorAsync(CreateActorDto createActorDto)
        {
            return _marketParticipantActorClient.CreateActorAsync(createActorDto);
        }

        public Task UpdateActorAsync(Guid actorId, ChangeActorDto changeActorDto)
        {
            return _marketParticipantActorClient.UpdateActorAsync(actorId, changeActorDto);
        }

        public Task<IEnumerable<GridAreaDto>> GetGridAreasAsync()
        {
            return _marketParticipantGridAreaClient.GetGridAreasAsync();
        }

        public Task UpdateGridAreaAsync(ChangeGridAreaDto changes)
        {
            return _marketParticipantGridAreaClient.UpdateGridAreaAsync(changes);
        }

        public Task<IEnumerable<ActorContactDto>> GetContactsAsync(Guid actorId)
        {
            return _marketParticipantActorContactClient.GetContactsAsync(actorId);
        }

        public Task<Guid> CreateContactAsync(Guid actorId, CreateActorContactDto contactDto)
        {
            return _marketParticipantActorContactClient.CreateContactAsync(actorId, contactDto);
        }

        public Task DeleteContactAsync(Guid actorId, Guid contactId)
        {
            return _marketParticipantActorContactClient.DeleteContactAsync(actorId, contactId);
        }

        public Task<IEnumerable<GridAreaOverviewItemDto>> GetGridAreaOverviewAsync()
        {
            return _marketParticipantGridAreaOverviewClient.GetGridAreaOverviewAsync();
        }

        public Task<IEnumerable<GridAreaAuditLogEntryDto>> GetGridAreaAuditLogEntriesAsync(Guid gridAreaId)
        {
            return _marketParticipantGridAreaClient.GetGridAreaAuditLogEntriesAsync(gridAreaId);
        }

        public Task<GetTokenResponseDto> GetTokenAsync(GetTokenRequestDto request)
        {
            return _tokenClient.GetTokenAsync(request);
        }

        public Task<GetAssociatedUserActorsResponseDto> GetUserActorsAsync(string accessToken)
        {
            return _marketParticipantUserClient.GetUserActorsAsync(accessToken);
        }

        public Task<GetAssociatedUserActorsResponseDto> GetUserActorsAsync(Guid userId)
        {
            return _marketParticipantUserClient.GetUserActorsAsync(userId);
        }

        public Task<UserDto> GetUserAsync(Guid userId)
        {
            return _marketParticipantUserClient.GetUserAsync(userId);
        }

        public Task<UserAuditLogsDto> GetUserAuditLogsAsync(Guid userId)
        {
            return _marketParticipantUserClient.GetUserAuditLogsAsync(userId);
        }

        public Task<UserOverviewResultDto> SearchUsersAsync(
            int pageNumber,
            int pageSize,
            UserOverviewSortProperty sortProperty,
            SortDirection sortDirection,
            UserOverviewFilterDto filter)
        {
            return _marketParticipantUserOverviewClient.SearchUsersAsync(
                pageNumber,
                pageSize,
                sortProperty,
                sortDirection,
                filter);
        }

        public Task<IEnumerable<SelectionActorDto>> GetSelectionActorsAsync()
        {
            return _marketParticipantActorQueryClient.GetSelectionActorsAsync();
        }
    }
}
