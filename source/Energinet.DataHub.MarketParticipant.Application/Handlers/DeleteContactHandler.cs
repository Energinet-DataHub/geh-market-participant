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
using Energinet.DataHub.MarketParticipant.Application.Commands.Contact;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Utilities;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers
{
    public sealed class DeleteContactHandler : IRequestHandler<DeleteContactCommand>
    {
        private readonly IOrganizationExistsHelperService _organizationExistsHelperService;
        private readonly IContactRepository _contactRepository;

        public DeleteContactHandler(
            IOrganizationExistsHelperService organizationExistsHelperService,
            IContactRepository contactRepository)
        {
            _organizationExistsHelperService = organizationExistsHelperService;
            _contactRepository = contactRepository;
        }

        public async Task<Unit> Handle(DeleteContactCommand request, CancellationToken cancellationToken)
        {
            Guard.ThrowIfNull(request, nameof(request));

            await _organizationExistsHelperService
                .EnsureOrganizationExistsAsync(request.OrganizationId)
                .ConfigureAwait(false);

            var contact = await _contactRepository
                .GetAsync(new ContactId(request.ContactId))
                .ConfigureAwait(false);

            if (contact == null)
            {
                return Unit.Value;
            }

            await _contactRepository
                .RemoveAsync(contact)
                .ConfigureAwait(false);

            return Unit.Value;
        }
    }
}
