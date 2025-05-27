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

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using Microsoft.IdentityModel.Tokens;
using NodaTime;
using JsonWebKey = Azure.Security.KeyVault.Keys.JsonWebKey;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Security;

[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "SigningKeyRing is a singleton and SemaphoreSlim has a finalizer.")]
internal sealed class SigningKeyRing : ISigningKeyRing
{
    private readonly IClock _clock;
    private readonly KeyClient _keyClient;
    private readonly string _keyName;

    private readonly SemaphoreSlim _cacheLock = new(1);
    private readonly TimeSpan _cacheLifeTime = TimeSpan.FromMinutes(7);

    private DateTimeOffset _lastCache = DateTimeOffset.MinValue;
    private volatile List<KeyVaultKey>? _keyCache;

    public SigningKeyRing(
        IClock clock,
        KeyClient keyClient,
        string keyName)
    {
        _clock = clock;
        _keyClient = keyClient;
        _keyName = keyName;
    }

    public string Algorithm => SecurityAlgorithms.RsaSha256;

    public async Task<CryptographyClient> GetSigningClientAsync()
    {
        var keyCache = await LoadKeysAsync().ConfigureAwait(false);
        var currentTime = _clock
            .GetCurrentInstant()
            .ToDateTimeOffset();

        var latestKey = keyCache
            .Where(key => !key.Properties.NotBefore.HasValue || key.Properties.NotBefore <= currentTime)
            .Where(key => !key.Properties.ExpiresOn.HasValue || key.Properties.ExpiresOn > currentTime)
            .FirstOrDefault(key => !key.Properties.CreatedOn.HasValue || key.Properties.CreatedOn < currentTime.AddMinutes(-10));

        if (latestKey == null)
        {
            var keyStrings = keyCache.Select(k => $"({k.Id}; NBF: {k.Properties.NotBefore}; EXP: {k.Properties.ExpiresOn}; CRE: {k.Properties.CreatedOn})");
            throw new InvalidOperationException($"No suitable key found in key cache. Keys: {string.Join(", ", keyStrings)}");
        }

        return _keyClient.GetCryptographyClient(_keyName, latestKey.Properties.Version);
    }

    public async Task<IEnumerable<JsonWebKey>> GetKeysAsync()
    {
        var keyCache = await LoadKeysAsync().ConfigureAwait(false);
        return keyCache.Select(vaultKey => vaultKey.Key);
    }

    private async Task<IReadOnlyCollection<KeyVaultKey>> LoadKeysAsync()
    {
        if (IsCacheValid())
            return _keyCache;

        await _cacheLock.WaitAsync().ConfigureAwait(false);

        if (IsCacheValid())
        {
            _cacheLock.Release();
            return _keyCache;
        }

        try
        {
            var keyVersions = _keyClient.GetPropertiesOfKeyVersionsAsync(_keyName);
            var keys = new List<KeyVaultKey>();

            var currentTime = _clock
                .GetCurrentInstant()
                .ToDateTimeOffset();

            await foreach (var keyDescription in keyVersions.ConfigureAwait(false))
            {
                if (keyDescription.Enabled != true)
                    continue;

                if (keyDescription.ExpiresOn < currentTime)
                    continue;

                var keyVersion = await _keyClient
                    .GetKeyAsync(keyDescription.Name, keyDescription.Version)
                    .ConfigureAwait(false);

                keys.Add(keyVersion.Value);
            }

            _keyCache = keys;
            _lastCache = DateTimeOffset.UtcNow;
            return _keyCache;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    [MemberNotNullWhen(true, nameof(_keyCache))]
    private bool IsCacheValid()
    {
        return _keyCache != null && _lastCache + _cacheLifeTime > DateTimeOffset.UtcNow;
    }
}
