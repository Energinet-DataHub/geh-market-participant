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
using Energinet.DataHub.MarketParticipant.Application.Commands.Actor;
using Energinet.DataHub.MarketParticipant.EntryPoint.Organization.Extensions;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.Organization.Functions
{
    public sealed class UpdateActorFunction
    {
        private readonly IMediator _mediator;

        public UpdateActorFunction(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Function("UpdateActor")]
        public Task<HttpResponseData> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put")]
            HttpRequestData request)
        {
            return request.ProcessAsync(async () =>
            {
                var updateActorCommand = await UpdateActorCommandAsync(request).ConfigureAwait(false);

                await _mediator
                    .Send(updateActorCommand)
                    .ConfigureAwait(false);

                return request.CreateResponse(HttpStatusCode.OK);
            });
        }

        private static async Task<UpdateActorCommand> UpdateActorCommandAsync(HttpRequestData request)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            try
            {
                var marketRoles = await JsonSerializer
                    .DeserializeAsync<MarketRoleDto[]>(request.Body, options)
                    .ConfigureAwait(false) ?? Array.Empty<MarketRoleDto>();

                var query = System.Web.HttpUtility.ParseQueryString(request.Url.Query);
                var organizationId = query.Get("organizationId") ?? string.Empty;
                var actorId = query.Get("actorId") ?? string.Empty;
                var gln = query.Get("gln") ?? string.Empty;
                var status = query.Get("status") ?? string.Empty;

                if (!Guid.TryParse(organizationId, out var orgGuid))
                    throw new ValidationException("Invalid organizationId, must be a valid GUID");
                if (!Guid.TryParse(actorId, out var actorGuid))
                    throw new ValidationException("Invalid actorId, must be a valid GUID");

                return new UpdateActorCommand(orgGuid, actorGuid, new ChangeActorDto(new GlobalLocationNumberDto(gln), status, marketRoles));
            }
            catch (JsonException)
            {
                throw new ValidationException("The body of the request could not be read.");
            }
        }
    }
}
