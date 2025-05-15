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

using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using Energinet.DataHub.MarketParticipant.Authorization.Model;
using Energinet.DataHub.MarketParticipant.Authorization.Model.AccessValidationRequests;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MarketParticipant.Authorization.Services
{
    public sealed class AuthorizationVerifyService : IVerifyAuthorization
    {
        private readonly CryptographyClient _cryptoClient;
        private readonly ILogger<AuthorizationVerifyService> _logger;

        public AuthorizationVerifyService(
            Uri keyVault,
            string keyName,
            ILogger<AuthorizationVerifyService> logger)
        {
            var keyClient = new KeyClient(keyVault, new DefaultAzureCredential());
            KeyVaultKey key = keyClient.GetKey(keyName);
            _logger = logger;

            // Todo:
            // Because of keyRotation, multiple versions of the key can be in use.
            // The versions used should be stored in an array or something like that and updated overtime.
            // Create signature should always simply use the current version.
            // Verify should use the version it gets from a input parameter.
            // Currently it just load once the current version from the key vault.
            // _key.Properties.Version
            // _cryptoClient = _keyClient.GetCryptographyClient(_keyName, "KeyVersion");
            _cryptoClient = new CryptographyClient(key.Id, new DefaultAzureCredential());
        }

        public async Task<bool> VerifySignatureAsync(AccessValidationRequest validationRequest, Signature signature)
        {
            ArgumentNullException.ThrowIfNull(validationRequest);
            ArgumentNullException.ThrowIfNull(signature);

            var signatureRequest = new VerifyRequest(signature.Expires);
            foreach (var signatureParam in validationRequest.GetSignatureParams())
            {
                signatureRequest.AddSignatureParameter(signatureParam);
            }

            var conversionResult = Convert.FromBase64String(signature.Value);
            var verifyResult = await _cryptoClient.VerifyDataAsync(SignatureAlgorithm.RS256, signatureRequest.CreateSignatureParamBytes(), conversionResult).ConfigureAwait(false);
            return verifyResult.IsValid;
        }
    }
}
