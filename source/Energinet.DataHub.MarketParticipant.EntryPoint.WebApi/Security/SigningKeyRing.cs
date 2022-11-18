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
using System.Threading.Tasks;
using Azure.Core;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using Microsoft.IdentityModel.Tokens;
using JsonWebKey = Azure.Security.KeyVault.Keys.JsonWebKey;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Security;

public sealed class SigningKeyRing : ISigningKeyRing
{
    private readonly KeyClient _keyClient;
    private readonly string _keyName;

    public SigningKeyRing(Uri keyVaultAddress, TokenCredential keyVaultCredential, string keyName)
    {
        _keyClient = new KeyClient(keyVaultAddress, keyVaultCredential);
        _keyName = keyName;
    }

    public string Algorithm => SecurityAlgorithms.RsaSha256;

    public Task<CryptographyClient> GetSigningClientAsync()
    {
        return Task.FromResult(_keyClient.GetCryptographyClient(_keyName));
    }

    public async Task<IEnumerable<JsonWebKey>> GetKeysAsync()
    {
        var keyVersions = _keyClient.GetPropertiesOfKeyVersionsAsync(_keyName);
        var keys = new List<JsonWebKey>();

        await foreach (var keyDescription in keyVersions)
        {
            if (keyDescription.Enabled != true)
                continue;

            var keyVersion = await _keyClient
                .GetKeyAsync(keyDescription.Name, keyDescription.Version)
                .ConfigureAwait(false);

            keys.Add(keyVersion.Value.Key);
        }

        return keys;
    }
}
