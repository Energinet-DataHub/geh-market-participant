﻿// Copyright 2020 Energinet DataHub A/S
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
using Energinet.DataHub.MarketParticipant.Authorization.Restriction;

namespace Energinet.DataHub.MarketParticipant.Authorization.Services
{
    public sealed class AuthorizationService : IAuthorizationService
    {
        private readonly KeyClient _keyClient;
        private readonly Uri _keyVault;
        private readonly string _keyName;
        private readonly KeyVaultKey _key;
        private readonly CryptographyClient _cryptoClient;

        public AuthorizationService(Uri keyVault, string keyName)
        {
            _keyVault = keyVault;
            _keyName = keyName;
            _keyClient = new KeyClient(keyVault, new DefaultAzureCredential());
            _key = _keyClient.GetKey(_keyName);

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
        public async Task<RestrictionSignatureDto> CreateSignatureAsync()
        {
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
    }
}
