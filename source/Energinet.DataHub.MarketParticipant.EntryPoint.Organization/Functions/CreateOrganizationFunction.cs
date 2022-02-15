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
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands;
using Energinet.DataHub.MarketParticipant.Utilities;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.Organization.Functions
{
    public class CreateOrganizationFunction
    {
        private readonly IMediator _mediator;
        private readonly ILogger<CreateOrganizationFunction> _logger;

        public CreateOrganizationFunction(
            IMediator mediator,
            ILogger<CreateOrganizationFunction> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [Function("CreateOrganization")]
        public async Task<HttpResponseData> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")]
            HttpRequestData request)
        {
            Guard.ThrowIfNull(request, nameof(request));

            try
            {
                var createOrganizationCommand = await CreateOrganizationCommandAsync(request).ConfigureAwait(false);
                if (createOrganizationCommand is null)
                {
                    throw new FluentValidation.ValidationException("Invalid arguments");
                }

                await _mediator.Send(createOrganizationCommand).ConfigureAwait(false);

                var response = request.CreateResponse(HttpStatusCode.OK);
                return response;
            }
            catch (FluentValidation.ValidationException e)
            {
                _logger.LogError("ValidationException in CreateOrganization: {message}", e.Message);
                var response = request.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync(e.Message).ConfigureAwait(false);
                return response;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                _logger.LogError("Error in CreateOrganization: {message}", ex.Message);
                return request.CreateResponse(HttpStatusCode.InternalServerError);
            }
        }

        private static async Task<CreateOrganizationCommand?> CreateOrganizationCommandAsync(HttpRequestData request)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            return await JsonSerializer
                .DeserializeAsync<CreateOrganizationCommand>(request.Body, options)
                .ConfigureAwait(false);
        }
    }
}
