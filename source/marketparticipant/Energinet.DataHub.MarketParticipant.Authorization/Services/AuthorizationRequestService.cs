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

using System.Net.Http.Json;
using Energinet.DataHub.MarketParticipant.Authorization.Model;
using Energinet.DataHub.MarketParticipant.Authorization.Model.AccessValidationRequests;

namespace Energinet.DataHub.MarketParticipant.Authorization.Services
{
    public sealed class AuthorizationRequestService : IRequestAuthorization
    {
        private readonly HttpClient _apiHttpClient;

        public AuthorizationRequestService(
            HttpClient apiHttpClient)
        {
            _apiHttpClient = apiHttpClient;
        }

        public async Task<Signature> RequestSignatureAsync(AccessValidationRequest accessValidationRequest)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, string.Empty);
            request.Content = JsonContent.Create(accessValidationRequest);
            using var response = await _apiHttpClient.SendAsync(request).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            var result = await response.Content
                .ReadFromJsonAsync<Signature>()
                .ConfigureAwait(false) ?? throw new InvalidOperationException("Failed to deserialize signature response content");
            return result;
        }
    }
}
