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
using System.Threading;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using Energinet.DataHub.MarketParticipant.Authorization.Model;
using Energinet.DataHub.MarketParticipant.Authorization.Model.AccessValidationRequests;
using Energinet.DataHub.MarketParticipant.Authorization.Services;
using Energinet.DataHub.MarketParticipant.Authorization.Services.AccessValidators;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MarketParticipant.Application.Security;

public class AuthorizationService
{
    private readonly ILogger<AuthorizationService> _logger;
    private readonly KeyVaultKey _key;
    private readonly CryptographyClient _cryptoClient;

    public AuthorizationService(
        ILogger<AuthorizationService> logger,
        KeyVaultKey key)
    {
        ArgumentNullException.ThrowIfNull(key);

        _logger = logger;
        _key = key;
        _cryptoClient = new CryptographyClient(key.Id, new DefaultAzureCredential());
    }

    public async Task<Signature> CreateSignatureAsync(AccessValidationRequest accessValidationRequest, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(accessValidationRequest);

        var validator = GetAccessValidator(accessValidationRequest);

        if (!validator.Validate())
            throw new ArgumentException("CreateSignatureAsync: caller was not authorized to the requested resource");

        var expires = DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeMilliseconds();
        var signatureRequest = new SignatureRequest(expires);
        foreach (var signatureParam in accessValidationRequest.GetSignatureParams())
        {
            signatureRequest.AddSignatureParameter(signatureParam);
        }

        var signResult = await _cryptoClient.SignDataAsync(SignatureAlgorithm.RS256, signatureRequest.CreateSignatureParamBytes(), cancellationToken).ConfigureAwait(false);

        return new Signature
        {
            Value = Convert.ToBase64String(signResult.Signature),
            KeyVersion = _key.Properties.Version,
            Expires = expires
        };
    }

    private static MeteringPointMasterDataAccessValidation GetAccessValidator(AccessValidationRequest accessValidationRequest)
    {
        return accessValidationRequest switch
        {
            MeteringPointMasterDataAccessValidationRequest meteringPointMasterDataAccessValidationRequest =>
                new MeteringPointMasterDataAccessValidation(meteringPointMasterDataAccessValidationRequest),
            _ => throw new ArgumentOutOfRangeException(nameof(accessValidationRequest))
        };
    }
}
