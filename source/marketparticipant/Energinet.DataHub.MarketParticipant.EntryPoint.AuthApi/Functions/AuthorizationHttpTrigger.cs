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
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Authorization.Model.AccessValidationRequests;
using Energinet.DataHub.MarketParticipant.EntryPoint.AuthApi.Security;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.AuthApi.Functions;

public sealed class AuthorizationHttpTrigger
{
    private const string BlockSignatureAuthorizationFeatureKey = "BlockSignatureAuthorization";

    private readonly IFeatureManager _featureManager;
    private readonly AuthorizationService _authorizationService;
    private readonly ILogger<AuthorizationHttpTrigger> _logger;

    public AuthorizationHttpTrigger(
        IFeatureManager featureManager,
        AuthorizationService authorizationService,
        ILogger<AuthorizationHttpTrigger> logger)
    {
        _featureManager = featureManager;
        _authorizationService = authorizationService;
        _logger = logger;
    }

    [Function("CreateSignature")]
    public async Task<HttpResponseData> CreateSignatureAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "createSignature")]
        string validationRequestJson,
        HttpRequestData httpRequest)
    {
        var blockSignatureAuthorization = await _featureManager
            .IsEnabledAsync(BlockSignatureAuthorizationFeatureKey)
            .ConfigureAwait(false);

        if (blockSignatureAuthorization)
            throw new UnauthorizedAccessException("Signature authorization is not allowed.");

        var accessValidationRequest = DeserializeAccessValidationRequest(validationRequestJson);
        if (accessValidationRequest == null)
        {
            _logger.LogDebug("Failed to deserialize access validation request");
            throw new ArgumentException("CreateSignatureAsync: Invalid validation request string");
        }

        var result = await _authorizationService
            .CreateSignatureAsync(accessValidationRequest, CancellationToken.None)
            .ConfigureAwait(false);

        HttpResponseData response;
        response = httpRequest.CreateResponse(HttpStatusCode.OK);
        await response
            .WriteAsJsonAsync(result)
            .ConfigureAwait(false);

        return response;
    }

    private AccessValidationRequest? DeserializeAccessValidationRequest(string validationRequestJson)
    {
        try
        {
            var accessValidationRequest = JsonSerializer.Deserialize<AccessValidationRequest>(validationRequestJson);

            return accessValidationRequest;
        }
        catch (JsonException jsonEx)
        {
            _logger.LogDebug(jsonEx, "Failed to deserialize validation request JSON");
        }
        catch (InvalidOperationException invalidOpEx)
        {
            _logger.LogDebug(invalidOpEx, "An invalid operation occurred during access validation");
        }
        catch (Exception ex) when (ex is ArgumentNullException or ArgumentException)
        {
            _logger.LogDebug(ex, "An argument-related error occurred during access validation");
        }

        return null;
    }
}
