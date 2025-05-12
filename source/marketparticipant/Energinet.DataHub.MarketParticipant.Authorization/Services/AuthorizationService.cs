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
using System.Text.Json;
using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using Energinet.DataHub.MarketParticipant.Authorization.Model.AccessValidationRequests;
using Energinet.DataHub.MarketParticipant.Authorization.Restriction;
using Energinet.DataHub.MarketParticipant.Authorization.Services.Factories;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MarketParticipant.Authorization.Services
{
    public sealed class AuthorizationService : IAuthorizationService
    {
        private readonly KeyClient _keyClient;
        private readonly string _keyName;
        private readonly KeyVaultKey _key;
        private readonly CryptographyClient _cryptoClient;
        private readonly ILogger<AuthorizationService> _logger;

        public AuthorizationService(
            Uri keyVault,
            string keyName,
            ILogger<AuthorizationService> logger)
        {
            _keyName = keyName;
            _keyClient = new KeyClient(keyVault, new DefaultAzureCredential());
            _key = _keyClient.GetKey(_keyName);
            _logger = logger;

            // Todo:
            // Because of keyRotation, multiple versions of the key can be in use.
            // The versions used should be stored in an array or something like that and updated overtime.
            // Create signature should always simply use the current version.
            // Verify should use the version it gets from a input parameter.
            // Currently it just load once the current version from the key vault.
            // _key.Properties.Version
            // _cryptoClient = _keyClient.GetCryptographyClient(_keyName, "KeyVersion");
            _cryptoClient = new CryptographyClient(_key.Id, new DefaultAzureCredential());
        }

        public async Task<Signature> RequestSignatureAsync(AccessValidationRequest accessValidationRequest)
        {
            // 1. Call api to make authorization check.
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/request-signature");
            request.Content = JsonContent.Create(accessValidationRequest);
            using var httpClient = new HttpClient(); // TODO: Use DI to inject HttpClient
            using var response = await httpClient.SendAsync(request).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            var result = await response.Content
                .ReadFromJsonAsync<Signature>()
                .ConfigureAwait(false) ?? throw new InvalidOperationException("Failed to deserialize signature response content");
            return result;
        }

        public async Task<Signature> CreateSignatureAsync(string validationRequestJson)
        {
            ArgumentNullException.ThrowIfNull(validationRequestJson);
            var accessValidationRequest = DeserializeAccessValidationRequest(validationRequestJson);
            if (accessValidationRequest == null)
            {
                _logger.LogDebug("Failed to deserialize access validation request");
                throw new ArgumentException("CreateSignatureAsync: Invalid validation request string");
            }

            var validator = AccessValidatorFactory.GetAccessValidator(accessValidationRequest);
            if (validator == null)
            {
                _logger.LogDebug("No validator found for the given access validation request");
                throw new ArgumentException("CreateSignatureAsync: validator for the request does not exist");
            }

            if (!validator.Validate())
                throw new ArgumentException("CreateSignatureAsync: caller was not authorized to the requested resource");


            // 2. If authorization succesfull: Create a signature (Input: AuthorizationRestriction) if unautorised return null
            // For now just return a static signature
            // Will be later something like this:
            // Var binaryRestriction = restriction.ToByteArray();
            var signatureRequest = new SignatureRequest();
            foreach (var signatureParam in accessValidationRequest.GetSignatureParams())
            {
                signatureRequest.AddSignatureParameter(signatureParam);
            }

            var expires = DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeMilliseconds();
            signatureRequest.SetExpiration(expires);
            var signature = await _cryptoClient.SignDataAsync(SignatureAlgorithm.RS256, signatureRequest.CreateSignatureParamBytes()).ConfigureAwait(false);
            return new Signature
            {
                Value = Convert.ToBase64String(signature.Signature),
                KeyVersion = _key.Properties.Version,
                Expires = expires
            };
        }

        public async Task<bool> VerifySignatureAsync(SignatureRequest signatureRequest, string signature)
        {
            ArgumentNullException.ThrowIfNull(signatureRequest);

            var conversionResult = Convert.FromBase64String(signature);
            var verifyResult = await _cryptoClient.VerifyDataAsync(SignatureAlgorithm.RS256, signatureRequest.CreateSignatureParamBytes(), conversionResult).ConfigureAwait(false);
            return verifyResult.IsValid;
        }

        private AccessValidationRequest? DeserializeAccessValidationRequest(string validationRequestJson)
        {
            try
            {
                var accessValidationRequest = JsonSerializer.Deserialize<AccessValidationRequest>(validationRequestJson);

                return accessValidationRequest;
            }
            catch (JsonException jsonEx)
            {
                _logger.LogDebug(jsonEx, "Failed to deserialize validation request JSON");
            }
            catch (InvalidOperationException invalidOpEx)
            {
                _logger.LogDebug(invalidOpEx, "An invalid operation occurred during access validation");
            }
            catch (Exception ex) when (ex is ArgumentNullException or ArgumentException)
            {
                _logger.LogDebug(ex, "An argument-related error occurred during access validation");
            }

            return null;
        }
    }
}
