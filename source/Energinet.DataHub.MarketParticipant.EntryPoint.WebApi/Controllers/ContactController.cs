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
using Energinet.DataHub.MarketParticipant.Application.Commands.Contact;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Controllers
{
    [ApiController]
    [Route("organization")]
    public sealed class ContactController : ControllerBase
    {
        private readonly ILogger<ContactController> _logger;
        private readonly IMediator _mediator;

        public ContactController(ILogger<ContactController> logger, IMediator mediator)
        {
            _logger = logger;
            _mediator = mediator;
        }

        [HttpGet("{organizationId:guid}/contact")]
        public async Task<IActionResult> ListAllAsync(Guid organizationId)
        {
            return await this.ProcessAsync(
                async () =>
                    {
                        var getOrganizationsCommand = new GetContactsCommand(organizationId);

                        var response = await _mediator
                            .Send(getOrganizationsCommand)
                            .ConfigureAwait(false);

                        return Ok(response.Contacts);
                    },
                _logger).ConfigureAwait(false);
        }

        [HttpPost("{organizationId:guid}/contact")]
        public async Task<IActionResult> CreateContactAsync(Guid organizationId, CreateContactDto contactDto)
        {
            return await this.ProcessAsync(
                async () =>
                {
                    var createContactCommand = new CreateContactCommand(organizationId, contactDto);

                    var response = await _mediator
                        .Send(createContactCommand)
                        .ConfigureAwait(false);

                    return Ok(response.ContactId.ToString());
                },
                _logger).ConfigureAwait(false);
        }

        [HttpDelete("{organizationId:guid}/contact/{contactId:guid}")]
        public async Task<IActionResult> DeleteContactAsync(Guid organizationId, Guid contactId)
        {
            return await this.ProcessAsync(
                async () =>
                {
                    var deleteContactCommand = new DeleteContactCommand(organizationId, contactId);

                    var response = await _mediator
                        .Send(deleteContactCommand)
                        .ConfigureAwait(false);

                    return Ok(response);
                },
                _logger).ConfigureAwait(false);
        }
    }
}
