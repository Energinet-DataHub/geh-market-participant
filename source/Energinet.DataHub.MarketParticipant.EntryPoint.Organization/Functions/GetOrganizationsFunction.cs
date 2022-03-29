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

using System.Net;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Organization;
using Energinet.DataHub.MarketParticipant.EntryPoint.Organization.Extensions;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.Organization.Functions
{
    public sealed class GetOrganizationsFunction
    {
        private readonly IMediator _mediator;

        public GetOrganizationsFunction(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Function("GetOrganizations")]
        public Task<HttpResponseData> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")]
            HttpRequestData request)
        {
            return request.ProcessAsync(async () =>
            {
                var getOrganizationsCommand = new GetOrganizationsCommand();

                var response = await _mediator
                    .Send(getOrganizationsCommand)
                    .ConfigureAwait(false);

                var responseData = request.CreateResponse(HttpStatusCode.OK);

                await responseData
                    .WriteAsJsonAsync(response)
                    .ConfigureAwait(false);

                return responseData;
            });
        }
    }
}
