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

using Energinet.DataHub.MarketParticipant.Authorization.Helpers;
using Energinet.DataHub.MarketParticipant.Authorization.Model.Parameters;

namespace Energinet.DataHub.MarketParticipant.Authorization.Services;

public sealed class SignatureRequest
{
    private static readonly IComparer<byte[]> _signatureByteComparer = new SignatureByteComparer();
    private readonly List<SignatureParameter> _params = [];

    public SignatureRequest(DateTimeOffset expiration)
    {
        SetExpiration(expiration);
    }

    /// <summary>
    /// Creates the Byte array for representing the Signature params.
    /// </summary>
    /// <returns>A byte array representing the signature params.</returns>
    public byte[] CreateSignatureParamBytes()
    {
        var sortedParams = _params
            .OrderBy(i => i.ParameterData, _signatureByteComparer);

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

        _params.Add(signatureParameter);
    }

    private void SetExpiration(DateTimeOffset expiration)
    {
        ArgumentNullException.ThrowIfNull(expiration);

        SignatureParameter expirationParameter = SignatureParameter.FromDateTimeOffset(expiration);
        _params.Add(expirationParameter);
    }
}
