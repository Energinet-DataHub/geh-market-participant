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
using System.Threading.Tasks;
using Azure.Identity;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi;

public class KeyClient : IKeyClient
{
    private readonly string _keyName;
    private Azure.Security.KeyVault.Keys.KeyClient _keyClient;

    public KeyClient(string keyVault, string keyName)
    {
        _keyClient = new Azure.Security.KeyVault.Keys.KeyClient(new Uri(keyVault), new DefaultAzureCredential());
        _keyName = keyName;
    }

    public async Task<IEnumerable<Key>> GetKeysAsync()
    {
        var keyProps = _keyClient.GetPropertiesOfKeyVersionsAsync(_keyName);
        var keys = new List<Key>();

        await foreach (var keyProp in keyProps)
        {
            var vaultKey = (await _keyClient.GetKeyAsync(keyProp.Name, keyProp.Version).ConfigureAwait(false))?.Value;

            if (vaultKey == null)
                continue;

            keys.Add(new Key(vaultKey.Id, keyProp.Version, "RSA", "sig", vaultKey.Key.N, vaultKey.Key.E));
        }

        return keys;
    }

    public async Task<Key> GetKeyAsync()
    {
        var vaultKey = (await _keyClient.GetKeyAsync(_keyName).ConfigureAwait(false)).Value;

        return new Key(vaultKey.Id, vaultKey.Id.Segments.Last(), "RSA", "sig", vaultKey.Key.N, vaultKey.Key.E);
    }
}
