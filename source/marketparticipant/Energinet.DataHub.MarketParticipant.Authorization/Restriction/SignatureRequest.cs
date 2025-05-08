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

using System.Globalization;
using Energinet.DataHub.MarketParticipant.Authorization.Config;
using Energinet.DataHub.MarketParticipant.Authorization.Restriction.Helpers;
using Energinet.DataHub.MarketParticipant.Authorization.Restriction.Parameters;

namespace Energinet.DataHub.MarketParticipant.Authorization.Restriction;

public sealed class SignatureRequest()
{
    private static readonly IComparer<byte[]> _signatureByteComparer = new SignatureByteComparer();
    private List<SignatureParameter> _params = [];

    /// <summary>
    /// Creates the Byte array for representing the Signature params.
    /// </summary>
    /// <returns>A byte array representing the signature params.</returns>
    public byte[] CreateSignatureParamBytes()
    {
        var sortedParams = _params
            .OrderBy(i => i.Key)
            .ThenBy(i => i.ParameterData, _signatureByteComparer);

        var arrayLength = _params.Sum(i => i.ParameterData.Length);

        var byteArray = new byte[arrayLength];
        var offset = 0;
        foreach (var param in sortedParams)
        {
            Buffer.BlockCopy(param.ParameterData, 0, byteArray, offset, param.ParameterData.Length);
            offset += param.ParameterData.Length;
        }

        return byteArray;
    }

    public void AddSignatureParameter(SignatureParameter signatureParameter)
    {
        ArgumentNullException.ThrowIfNull(signatureParameter);

        if (ContainsKey(signatureParameter.Key)) throw new InvalidOperationException("Key already exists");

        if (_params.Any(i => KeyExistsWithDifferentType(i, signatureParameter)))
            throw new ArgumentException("Adding Param to signature failed, Param Key already exists with different type");

        _params.Add(signatureParameter);
    }

    internal void SetExpiration(long expiration)
    {
        ArgumentNullException.ThrowIfNull(expiration);

        if (ContainsKey(Settings.ExpirationKey)) throw new InvalidOperationException("Expiration already set");

        SignatureParameter expirationParameter = SignatureParameter.FromLong(expiration, Settings.ExpirationKey);

        if (_params.Any(i => KeyExistsWithDifferentType(i, expirationParameter)))
            throw new ArgumentException("Adding expiration Param to signature failed, Param Key already exists with different type");

        _params.Add(expirationParameter);
    }

    private static bool KeyExistsWithDifferentType(SignatureParameter existingEntry, SignatureParameter newEntry)
    {
        return existingEntry.Key == newEntry.Key && existingEntry.GetType() != newEntry.GetType();
    }

    private bool ContainsKey(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        var internalKey = key.ToUpper(CultureInfo.InvariantCulture);
        return _params.Any(i => i.Key == internalKey);
    }
}
