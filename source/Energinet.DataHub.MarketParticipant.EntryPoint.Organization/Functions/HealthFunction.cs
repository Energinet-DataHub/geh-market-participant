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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.EntryPoint.Organization.HealthCheck;
using Energinet.DataHub.MarketParticipant.Infrastructure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.Organization.Functions
{
    public sealed class HealthFunction
    {
        private const string FunctionName = nameof(HealthFunction);

        private readonly ServiceBusConfig _serviceBusConfig;
        private readonly DatabaseConfig _databaseConfig;
        private readonly IHealth _health;

        public HealthFunction(
            ServiceBusConfig serviceBusConfig,
            DatabaseConfig databaseConfig,
            IHealth health)
        {
            _databaseConfig = databaseConfig;
            _serviceBusConfig = serviceBusConfig;
            _health = health;
        }

        [Function(FunctionName)]
        public async Task<HttpResponseData> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData request)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));

            var results = await _health
                .CreateFluentValidator()
                .AddMessageBus(
                    "SBT_MARKET_PARTICIPANT_CHANGED_NAME",
                    _serviceBusConfig.ConnectionString,
                    _serviceBusConfig.IntegrationEventsTopicName)
                .AddSqlDatabase(
                    "SQL_MP_DB_CONNECTION_STRING",
                    _databaseConfig.ConnectionString)
                .RunInParallelAsync()
                .ConfigureAwait(false);

            var response = request.CreateResponse();
            response.StatusCode = CalculateStatusCode(results);
            response.Body = new MemoryStream(Encoding.UTF8.GetBytes(Print(results)));
            response.Headers.Add("Content-Type", "text/plain");
            return response;
        }

        private static HttpStatusCode CalculateStatusCode(IEnumerable<(string Key, bool Result)> healthChecks)
        {
            return healthChecks.All(x => x.Result) ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable;
        }

        private static string Print(IEnumerable<(string Key, bool Result)> healthChecks)
        {
            var output = new StringBuilder();

            var longestString = 0;
            var isSuccess = true;

            foreach (var healthCheck in healthChecks)
            {
                var initialLength = output.Length;

                if (healthCheck.Result)
                {
                    output.AppendFormat(CultureInfo.InvariantCulture, "{0}: OK\n", healthCheck.Key);
                }
                else
                {
                    output.AppendFormat(CultureInfo.InvariantCulture, "{0}: FAILED\n", healthCheck.Key);
                    isSuccess = false;
                }

                longestString = Math.Max(longestString, output.Length - initialLength);
            }

            output.Insert(0, new string('=', longestString) + "\n");
            output.Insert(0, isSuccess ? "SUCCESS\n" : "FAILURE\n");

            return output.ToString();
        }
    }
}
