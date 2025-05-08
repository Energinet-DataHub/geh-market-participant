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

using System.Text.Json;
using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using Energinet.DataHub.MarketParticipant.Authorization.Model;
using Energinet.DataHub.MarketParticipant.Authorization.Restriction;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MarketParticipant.Authorization.Services
{
    public sealed class AuthorizationService : IAuthorizationService
    {
        private readonly KeyClient _keyClient;
        private readonly Uri _keyVault;
        private readonly string _keyName;
        private readonly KeyVaultKey _key;
        private readonly CryptographyClient _cryptoClient;
        private readonly ILogger<AuthorizationService> _logger;

        public AuthorizationService(Uri keyVault, string keyName, ILogger<AuthorizationService> logger)
        {
            _keyVault = keyVault;
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

        // Later this task has AuthorizationRestriction and UserIdentification as input
        public async Task<RestrictionSignatureDto> CreateSignatureAsync(string accessValidation)
        {
            if (!ValidateAccess(accessValidation))
                throw new ArgumentException("Invalid request");

            // 1. Call api to make authorization check. (Input: AuthorizationRestriction and UserIdentification)
            // 2. If authorization succesfull: Create a signature (Input: AuthorizationRestriction) if unautorised return null
            // For now just return a static signature
            // Will be later something like this:
            // Var binaryRestriction = restriction.ToByteArray();
            byte[] binaryRestriction = [1, 2, 3, 4];
            var signature = await _cryptoClient.SignDataAsync(SignatureAlgorithm.RS256, binaryRestriction).ConfigureAwait(false);
            return new RestrictionSignatureDto(Convert.ToBase64String(signature.Signature));
        }

        public async Task<bool> VerifySignatureAsync(AuthorizationRestriction restriction, string signature)
        {
            // Will be later something like this:
            // Var binaryRestriction = restriction.ToByteArray();
            // For now Static
            byte[] binaryRestriction = [1, 2, 3, 4];
            var conversionResult = Convert.FromBase64String(signature);
            var verifyResult = await _cryptoClient.VerifyDataAsync(SignatureAlgorithm.RS256, binaryRestriction, conversionResult).ConfigureAwait(false);
            return verifyResult.IsValid;
        }

        private bool ValidateAccess(string access)
        {
            var isValid = false;

            try
            {
                if (!string.IsNullOrEmpty(access))
                {
                    var signatureBytes = new byte[access.Length];
                    if (Convert.TryFromBase64String(access, signatureBytes, out var bytesWritten))
                    {
                        var data = Convert.FromBase64String(access);
                        var decodedString = System.Text.Encoding.UTF8.GetString(data);
                        var accessValidation = DeserializeAccessValidation(decodedString)!;

                        if (accessValidation != null)
                        {
                            isValid = accessValidation.Validate();
                        }
                        else
                        {
                            _logger.LogDebug("Deserializing access validation failed, accessValidation is null");
                        }
                    }
                }
            }
            catch (FormatException e)
            {
                _logger.LogDebug(e, "Deserializing access validation failed due to invalid format, inner message: {EInnerException}", e.InnerException);
            }

            return isValid;
        }

        private IAccessValidation? DeserializeAccessValidation(string jsonString)
        {
            try
            {
                return JsonSerializer.Deserialize<IAccessValidation>(jsonString);
            }
            catch (JsonException)
            {
                return null;
            }
            catch (NotSupportedException)
            {
                return null;
            }
        }
    }
}
