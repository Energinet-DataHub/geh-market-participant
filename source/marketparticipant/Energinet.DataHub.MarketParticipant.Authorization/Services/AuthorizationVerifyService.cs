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
using Energinet.DataHub.MarketParticipant.Authorization.Model.AccessValidationVerify;

namespace Energinet.DataHub.MarketParticipant.Authorization.Services;

public sealed class AuthorizationVerifyService : IVerifyAuthorization
{
    private readonly KeyClient _keyClient;
    private readonly string _keyName;

    public AuthorizationVerifyService(Uri keyVault, string keyName)
    {
        _keyName = keyName;
        _keyClient = new KeyClient(keyVault, new DefaultAzureCredential());
    }

    public async Task<bool> VerifySignatureAsync(AccessValidationVerifyRequest verifyRequest, Signature signature)
    {
        ArgumentNullException.ThrowIfNull(verifyRequest);
        ArgumentNullException.ThrowIfNull(signature);

        var signatureRequest = new VerifyRequest(signature.Expires, signature.RequestId);
        foreach (var signatureParam in verifyRequest.GetSignatureParams())
        {
            signatureRequest.AddSignatureParameter(signatureParam);
        }

        var conversionResult = Convert.FromBase64String(signature.Value);
        var cryptoClient = _keyClient.GetCryptographyClient(_keyName, signature.KeyVersion);
        var verifyResult = await cryptoClient.VerifyDataAsync(SignatureAlgorithm.RS256, signatureRequest.CreateSignatureParamBytes(), conversionResult).ConfigureAwait(false);
        return verifyResult.IsValid;
    }
}
