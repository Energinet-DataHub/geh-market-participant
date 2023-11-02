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
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Azure.Core;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.CertificateSynchronization.Services;

public sealed class ApimCertificateStore : IApimCertificateStore
{
    private const string ApimApiVersion = "2022-08-01";

    private readonly Uri _targetKeyVaultUri;
    private readonly TokenCredential _apimSpCredential;
    private readonly IHttpClientFactory _httpClientFactory;

    private AccessToken? _accessToken;

    public ApimCertificateStore(
        Uri targetKeyVaultUri,
        TokenCredential apimSpCredential,
        IHttpClientFactory httpClientFactory)
    {
        _targetKeyVaultUri = targetKeyVaultUri;
        _apimSpCredential = apimSpCredential;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IReadOnlyCollection<CertificateIdentifier>> GetCertificateIdentifiersAsync()
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, new Uri(string.Empty, UriKind.Relative));
        using var httpResponse = await SendAsync(httpRequest).ConfigureAwait(false);

        var certificateList = await httpResponse
            .Content
            .ReadFromJsonAsync<ApimCertificateList>()
            .ConfigureAwait(false);

        var apimCertificates = certificateList ?? throw new InvalidOperationException("Failed to deserialize GET /certificates response from APIM.");
        var certificateIdentifiers = new List<CertificateIdentifier>();

        foreach (var apimCertificate in apimCertificates.Value)
        {
            var kv = apimCertificate.Properties.KeyVault;
            if (kv == null || !_targetKeyVaultUri.IsBaseOf(kv.SecretIdentifier))
                continue;

            var certificateIdentifier = new CertificateIdentifier(
                kv.SecretIdentifier,
                apimCertificate.Name,
                !kv.IsValid);

            certificateIdentifiers.Add(certificateIdentifier);
        }

        return certificateIdentifiers;
    }

    public async Task AddCertificateAsync(CertificateIdentifier certificateIdentifier)
    {
        ArgumentNullException.ThrowIfNull(certificateIdentifier);

        var apimCertificateName = certificateIdentifier.Name;
        var apimCertificate = new ApimCertificate
        {
            Properties = new ApimCertificateProperties
            {
                KeyVault = new ApimCertificateFromKeyVault
                {
                    SecretIdentifier = certificateIdentifier.Id
                }
            }
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Put, new Uri(apimCertificateName, UriKind.Relative));
        httpRequest.Content = JsonContent.Create(apimCertificate);

        using var httpResponse = await SendAsync(httpRequest).ConfigureAwait(false);

        httpResponse.EnsureSuccessStatusCode();
    }

    public async Task RemoveCertificateAsync(CertificateIdentifier certificateIdentifier)
    {
        ArgumentNullException.ThrowIfNull(certificateIdentifier);

        var apimCertificateName = certificateIdentifier.Name;

        using var httpRequest = new HttpRequestMessage(HttpMethod.Delete, new Uri(apimCertificateName, UriKind.Relative));
        using var httpResponse = await SendAsync(httpRequest).ConfigureAwait(false);

        httpResponse.EnsureSuccessStatusCode();
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequest)
    {
        httpRequest.RequestUri = new Uri($"{httpRequest.RequestUri}?api-version={ApimApiVersion}", UriKind.Relative);

        var accessToken = await AcquireAccessTokenAsync().ConfigureAwait(false);

        using var httpClient = _httpClientFactory.CreateClient(nameof(HttpClient));
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Token);

        var httpResponse = await httpClient
            .SendAsync(httpRequest)
            .ConfigureAwait(false);

        httpResponse.EnsureSuccessStatusCode();

        return httpResponse;
    }

    private async ValueTask<AccessToken> AcquireAccessTokenAsync()
    {
        if (_accessToken.HasValue && _accessToken.Value.ExpiresOn >= DateTimeOffset.UtcNow.AddSeconds(10))
            return _accessToken.Value;

        var apimManagementScope = new TokenRequestContext(new[]
        {
            "https://management.azure.com//.default"
        });

        var newToken = await _apimSpCredential
            .GetTokenAsync(apimManagementScope, default)
            .ConfigureAwait(false);

        _accessToken = newToken;
        return newToken;
    }

    internal sealed class ApimCertificateList
    {
        public IEnumerable<ApimCertificate> Value { get; set; } = Enumerable.Empty<ApimCertificate>();
    }

    internal sealed class ApimCertificate
    {
        public string Name { get; set; } = null!;
        public ApimCertificateProperties Properties { get; set; } = null!;
    }

    internal sealed class ApimCertificateProperties
    {
        public ApimCertificateFromKeyVault? KeyVault { get; set; }
    }

    internal sealed class ApimCertificateFromKeyVault
    {
        public Uri SecretIdentifier { get; set; } = null!;
        public ApimCertificateKeyVaultStatus? LastStatus { get; set; }

        [JsonIgnore]
        public bool IsValid => LastStatus?.Code == "Success";
    }

    internal sealed class ApimCertificateKeyVaultStatus
    {
        public string Code { get; set; } = null!;
    }
}
