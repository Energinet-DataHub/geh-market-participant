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

using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands;
using Energinet.DataHub.MarketParticipant.EntryPoint.Organization.Extensions;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.Organization.Functions
{
    public sealed class AddOrganizationRoleFunction
    {
        private readonly IMediator _mediator;

        public AddOrganizationRoleFunction(IMediator mediator)
        {
            _mediator = mediator;
        }

        // TODO: Should this be REST?
        [Function("AddOrganizationRole")]
        public Task<HttpResponseData> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")]
            HttpRequestData request)
        {
            return request.ProcessAsync(async () =>
            {
                var addOrganizationRoleCommand = await AddOrganizationCommandAsync(request).ConfigureAwait(false);

                await _mediator
                    .Send(addOrganizationRoleCommand)
                    .ConfigureAwait(false);

                return request.CreateResponse(HttpStatusCode.OK);
            });
        }

        private static async Task<AddOrganizationRoleCommand> AddOrganizationCommandAsync(HttpRequestData request)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            try
            {
                var organizationRoleDto = await JsonSerializer
                    .DeserializeAsync<OrganizationRoleDto>(request.Body, options)
                    .ConfigureAwait(false) ?? new OrganizationRoleDto(string.Empty);

                var query = System.Web.HttpUtility.ParseQueryString(request.Url.Query);
                var organizationId = query.Get("organizationId") ?? string.Empty;

                return new AddOrganizationRoleCommand(organizationId, organizationRoleDto);
            }
            catch (JsonException)
            {
                throw new ValidationException("The body of the request could not be read.");
            }
        }
    }
}
