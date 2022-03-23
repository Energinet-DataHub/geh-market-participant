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
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.EntryPoint.Organization.Extensions;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.Organization.Functions
{
    public sealed class UpdateOrganizationFunction
    {
        private readonly IMediator _mediator;

        public UpdateOrganizationFunction(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Function("UpdateOrganization")]
        public Task<HttpResponseData> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put")]
            HttpRequestData request)
        {
            return request.ProcessAsync(async () =>
            {
                var updateOrganizationCommand = await UpdateOrganizationCommandAsync(request).ConfigureAwait(false);

                await _mediator
                    .Send(updateOrganizationCommand)
                    .ConfigureAwait(false);

                return request.CreateResponse(HttpStatusCode.OK);
            });
        }

        private static async Task<UpdateOrganizationCommand> UpdateOrganizationCommandAsync(HttpRequestData request)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            try
            {
                var organizationDto = await JsonSerializer
                    .DeserializeAsync<ChangeOrganizationDto>(request.Body, options)
                    .ConfigureAwait(false) ?? new ChangeOrganizationDto(string.Empty);

                var query = System.Web.HttpUtility.ParseQueryString(request.Url.Query);
                var organizationId = query.Get("organizationId") ?? string.Empty;

                if (!Guid.TryParse(organizationId, out var orgGuid))
                    throw new ValidationException("Invalid organizationId, must be a valid GUID");

                return new UpdateOrganizationCommand(orgGuid, organizationDto);
            }
            catch (JsonException)
            {
                throw new ValidationException("The body of the request could not be read.");
            }
        }
    }
}
