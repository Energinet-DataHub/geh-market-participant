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

using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Energinet.DataHub.MarketParticipant.Infrastructure.Services;
using Energinet.DataHub.MarketParticipant.Utilities;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers
{
    public sealed class CreateOrganizationHandler : IRequestHandler<CreateOrganizationCommand, CreateOrganizationResponse>
    {
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IOrganizationEventDispatcher _organizationEventDispatcher;
        private readonly IActiveDirectoryService _activeDirectoryService;

        public CreateOrganizationHandler(
            IOrganizationRepository organizationRepository,
            IOrganizationEventDispatcher organizationEventDispatcher,
            IActiveDirectoryService activeDirectoryService)
        {
            _organizationRepository = organizationRepository;
            _organizationEventDispatcher = organizationEventDispatcher;
            _activeDirectoryService = activeDirectoryService;
        }

        public async Task<CreateOrganizationResponse> Handle(CreateOrganizationCommand request, CancellationToken cancellationToken)
        {
            Guard.ThrowIfNull(request, nameof(request));

            var (name, gln) = request.Organization;

            var appRegistrationId = await _activeDirectoryService.EnsureAppRegistrationIdAsync(gln).ConfigureAwait(false);

            var organizationToSave = new Organization(
                appRegistrationId,
                new GlobalLocationNumber(gln),
                name);

            var createdId = await _organizationRepository
                .AddOrUpdateAsync(organizationToSave)
                .ConfigureAwait(false);

            var organizationWithId = new Organization(
                createdId,
                organizationToSave.ActorId,
                organizationToSave.Gln,
                organizationToSave.Name,
                organizationToSave.Roles);

            await _organizationEventDispatcher
                .DispatchChangedEventAsync(organizationWithId)
                .ConfigureAwait(false);

            return new CreateOrganizationResponse(createdId.Value.ToString());
        }
    }
}
