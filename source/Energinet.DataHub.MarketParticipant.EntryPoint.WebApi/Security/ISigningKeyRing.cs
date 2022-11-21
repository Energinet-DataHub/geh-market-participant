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

using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Security;

/// <summary>
/// Manages keys for signing JWT.
/// </summary>
public interface ISigningKeyRing
{
    /// <summary>
    /// The algorithm used to sign a JWT.
    /// </summary>
    string Algorithm { get; }

    /// <summary>
    /// Gets a signing client with the newest key.
    /// </summary>
    Task<CryptographyClient> GetSigningClientAsync();

    /// <summary>
    /// Returns all the valid public signing keys.
    /// </summary>
    Task<IEnumerable<JsonWebKey>> GetKeysAsync();
}
