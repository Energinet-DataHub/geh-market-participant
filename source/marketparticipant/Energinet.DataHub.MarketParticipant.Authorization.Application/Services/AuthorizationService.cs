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

using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using Energinet.DataHub.MarketParticipant.Authorization.Application.Factories;
using Energinet.DataHub.MarketParticipant.Authorization.Model;
using Energinet.DataHub.MarketParticipant.Authorization.Model.AccessValidationRequests;
using Energinet.DataHub.MarketParticipant.Authorization.Model.Parameters;
using Energinet.DataHub.MarketParticipant.Authorization.Services;

namespace Energinet.DataHub.MarketParticipant.Authorization.Application.Services;

public sealed class AuthorizationService
{
    private readonly KeyClient _keyClient;
    private readonly string _keyName;
    private readonly IAccessValidatorDispatchService _accessValidatorDispatchService;

    public AuthorizationService(
        KeyClient keyClient,
        string keyName,
        IAccessValidatorDispatchService accessValidatorDispatchService)
    {
        _keyName = keyName;
        _keyClient = keyClient;
        _accessValidatorDispatchService = accessValidatorDispatchService;
    }

    public async Task<Signature> CreateSignatureAsync(AccessValidationRequest accessValidationRequest, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(accessValidationRequest);

        var validationResponse = await _accessValidatorDispatchService.ValidateAsync(accessValidationRequest).ConfigureAwait(false);

        if (!validationResponse.Valid)
            throw new ArgumentException("CreateSignatureAsync: caller was not authorized to the requested resource");

        var signatureRequest = new SignatureRequest();
        foreach (var signatureParam in accessValidationRequest.GetSignatureParams())
        {
            signatureRequest.AddSignatureParameter(signatureParam);
        }

        if (validationResponse.ValidAccessPeriods != null)
        {
            signatureRequest.AddSignatureParameter(SignatureParameter.FromAccessPeriods(SignatureParamKeys.AccessPeriodsKey, validationResponse.ValidAccessPeriods));
        }

        var key = await _keyClient.GetKeyAsync(_keyName, cancellationToken: cancellationToken).ConfigureAwait(false);
        var cryptoClient = _keyClient.GetCryptographyClient(_keyName, key.Value.Properties.Version);
        var signResult = await cryptoClient.SignDataAsync(SignatureAlgorithm.RS256, signatureRequest.CreateSignatureParamBytes(), cancellationToken).ConfigureAwait(false);

        return new Signature
        {
            Value = Convert.ToBase64String(signResult.Signature),
            KeyVersion = key.Value.Properties.Version,
            Expires = signatureRequest.Expiration,
            RequestId = signatureRequest.RequestId,
            AccessPeriods = validationResponse.ValidAccessPeriods
        };
    }
}
