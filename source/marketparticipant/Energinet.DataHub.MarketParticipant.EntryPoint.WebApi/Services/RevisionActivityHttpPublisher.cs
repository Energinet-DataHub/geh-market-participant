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

using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Services;

public sealed class RevisionActivityHttpPublisher : IRevisionActivityPublisher
{
    private readonly ILogger<RevisionActivityHttpPublisher> _logger;
    private readonly IOptions<RevisionLogOptions> _revisionLogOptions;
    private readonly IHttpClientFactory _httpClientFactory;

    public RevisionActivityHttpPublisher(
        ILogger<RevisionActivityHttpPublisher> logger,
        IOptions<RevisionLogOptions> revisionLogOptions,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _revisionLogOptions = revisionLogOptions;
        _httpClientFactory = httpClientFactory;
    }

    public async Task PublishAsync(string message)
    {
        _logger.LogInformation("Posting message: " + message);

        using var logRequest = new HttpRequestMessage(HttpMethod.Post, _revisionLogOptions.Value.ApiAddress);
        logRequest.Content = new StringContent(message, Encoding.UTF8, MediaTypeNames.Application.Json);

        using var httpClient = _httpClientFactory.CreateClient("revision-log-http-client");
        using var response = await httpClient
            .SendAsync(logRequest)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
    }
}
