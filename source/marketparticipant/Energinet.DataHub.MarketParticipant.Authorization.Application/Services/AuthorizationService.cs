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
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using Energinet.DataHub.MarketParticipant.Authorization.Application.Authorization.AccessValidators;
using Energinet.DataHub.MarketParticipant.Authorization.Application.Authorization.Clients;
using Energinet.DataHub.MarketParticipant.Authorization.Model;
using Energinet.DataHub.MarketParticipant.Authorization.Model.AccessValidationRequests;
using Energinet.DataHub.MarketParticipant.Authorization.Services;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MarketParticipant.Authorization.Application.Services;

public class AuthorizationService
{
    private readonly KeyClient _keyClient;
    private readonly string _keyName;
    private readonly ILogger<AuthorizationService> _logger;
    private readonly IElectricityMarketClient _electricityMarketClient;

    public AuthorizationService(
        KeyClient keyClient,
        string keyName,
        ILogger<AuthorizationService> logger,
        IElectricityMarketClient electricityMarketClient)
    {
        _keyName = keyName;
        _keyClient = keyClient;
        _logger = logger;
        _electricityMarketClient = electricityMarketClient;
    }

    public async Task<Signature> CreateSignatureAsync(AccessValidationRequest accessValidationRequest, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(accessValidationRequest);

        var validator = GetAccessValidator(accessValidationRequest);

        if (!await validator.ValidateAsync().ConfigureAwait(false))
            throw new ArgumentException("CreateSignatureAsync: caller was not authorized to the requested resource");

        var signatureRequest = new SignatureRequest();
        foreach (var signatureParam in accessValidationRequest.GetSignatureParams())
        {
            signatureRequest.AddSignatureParameter(signatureParam);
        }

        var key = await _keyClient.GetKeyAsync(_keyName, cancellationToken: cancellationToken).ConfigureAwait(false);
        var cryptoClient = _keyClient.GetCryptographyClient(_keyName, key.Value.Properties.Version);
        var signResult = await cryptoClient.SignDataAsync(SignatureAlgorithm.RS256, signatureRequest.CreateSignatureParamBytes(), cancellationToken).ConfigureAwait(false);

        return new Signature
        {
            Value = Convert.ToBase64String(signResult.Signature),
            KeyVersion = key.Value.Properties.Version,
            Expires = signatureRequest.Expiration
        };
    }

    private MeteringPointMasterDataAccessValidation GetAccessValidator(AccessValidationRequest accessValidationRequest)
    {
        return accessValidationRequest switch
        {
            MeteringPointMasterDataAccessValidationRequest meteringPointMasterDataAccessValidationRequest =>
                new MeteringPointMasterDataAccessValidation(meteringPointMasterDataAccessValidationRequest, _electricityMarketClient),
            _ => throw new ArgumentOutOfRangeException(nameof(accessValidationRequest))
        };
    }
}
