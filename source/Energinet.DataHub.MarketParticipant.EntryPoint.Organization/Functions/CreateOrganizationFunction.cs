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
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands;
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
            try
            {
                var createOrganizationCommand = await CreateOrganizationCommandFromRequest(request);

                var (success, errorMessage) = await _mediator.Send(createOrganizationCommand).ConfigureAwait(false);

                var response = success
                    ? request.CreateResponse(HttpStatusCode.OK)
                    : request.CreateResponse(HttpStatusCode.BadRequest);

                if (!string.IsNullOrWhiteSpace(errorMessage)) await response.WriteStringAsync(errorMessage);

                return response;
            }
            catch (Exception e)
            {
                _logger.LogError("Error in CreateOrganization: {message}", e.Message);
                return request.CreateResponse(HttpStatusCode.InternalServerError);
            }
        }

        private static async Task<CreateOrganizationCommand?> CreateOrganizationCommandFromRequest(HttpRequestData request)
        {
            string requestBody;
            using (var streamReader = new StreamReader(request.Body))
            {
                requestBody = await streamReader.ReadToEndAsync();
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var createOrganizationCommand = JsonSerializer.Deserialize<CreateOrganizationCommand>(requestBody, options);
            return createOrganizationCommand;
        }
    }
}
