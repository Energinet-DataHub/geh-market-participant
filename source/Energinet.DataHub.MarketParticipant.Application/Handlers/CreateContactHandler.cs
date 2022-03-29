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

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Contact;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services.Rules;
using Energinet.DataHub.MarketParticipant.Utilities;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers
{
    public sealed class CreateContactHandler : IRequestHandler<CreateContactCommand, CreateContactResponse>
    {
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IContactRepository _contactRepository;
        private readonly IOverlappingContactCategoriesRuleService _overlappingContactCategoriesRuleService;

        public CreateContactHandler(
            IOrganizationRepository organizationRepository,
            IContactRepository contactRepository,
            IOverlappingContactCategoriesRuleService overlappingContactCategoriesRuleService)
        {
            _organizationRepository = organizationRepository;
            _contactRepository = contactRepository;
            _overlappingContactCategoriesRuleService = overlappingContactCategoriesRuleService;
        }

        public async Task<CreateContactResponse> Handle(CreateContactCommand request, CancellationToken cancellationToken)
        {
            Guard.ThrowIfNull(request, nameof(request));

            var organizationId = new OrganizationId(request.OrganizationId);

            await EnsureOrganizationExistsAsync(organizationId).ConfigureAwait(false);

            var existingContacts = await _contactRepository
                .GetAsync(organizationId)
                .ConfigureAwait(false);

            var contact = CreateContact(organizationId, request.Contact);

            _overlappingContactCategoriesRuleService
                .ValidateCategoriesAcrossContacts(existingContacts.Append(contact));

            var contactId = await _contactRepository
                .AddAsync(contact)
                .ConfigureAwait(false);

            return new CreateContactResponse(contactId.Value);
        }

        private static Contact CreateContact(OrganizationId organizationId, CreateContactDto contactDto)
        {
            var optionalPhoneNumber = contactDto.Phone == null
                ? null
                : new PhoneNumber(contactDto.Phone);

            return new Contact(
                organizationId,
                contactDto.Name,
                ContactCategory.FromName(contactDto.Category),
                new EmailAddress(contactDto.Email),
                optionalPhoneNumber);
        }

        private async Task EnsureOrganizationExistsAsync(OrganizationId organizationId)
        {
            var organization = await _organizationRepository
                .GetAsync(organizationId)
                .ConfigureAwait(false);

            if (organization == null)
            {
                throw new NotFoundValidationException(organizationId.Value);
            }
        }
    }
}
