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
using Energinet.DataHub.MarketParticipant.Application;
using Energinet.DataHub.MarketParticipant.Authorization.Model.AccessValidationRequests;
using Energinet.DataHub.MarketParticipant.EntryPoint.AuthApi.Security;
using Energinet.DataHub.RevisionLog.Integration;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using NodaTime;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.AuthApi.Functions;

public sealed class AuthorizationHttpTrigger
{
    private const string BlockSignatureAuthorizationFeatureKey = "BlockSignatureAuthorization";

    private readonly IFeatureManager _featureManager;
    private readonly AuthorizationService _authorizationService;
    private readonly ILogger<AuthorizationHttpTrigger> _logger;
    private readonly IRevisionLogClient _revisionLogClient;

    public AuthorizationHttpTrigger(
        IFeatureManager featureManager,
        AuthorizationService authorizationService,
        ILogger<AuthorizationHttpTrigger> logger,
        IRevisionLogClient revisionLogClient)
    {
        _featureManager = featureManager;
        _authorizationService = authorizationService;
        _logger = logger;
        _revisionLogClient = revisionLogClient;
    }

    [Function("CreateSignature")]
    public async Task<HttpResponseData> CreateSignatureAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "createSignature")]
        string validationRequestJson,
        HttpRequestData httpRequest)
    {
        ArgumentNullException.ThrowIfNull(httpRequest);

        var blockSignatureAuthorization = await _featureManager
            .IsEnabledAsync(BlockSignatureAuthorizationFeatureKey)
            .ConfigureAwait(false);

        if (blockSignatureAuthorization)
        {
            _logger.LogInformation("Rejecting request as signature authorization is blocked.");
            return httpRequest.CreateResponse(HttpStatusCode.Forbidden);
        }

        var accessValidationRequest = DeserializeAccessValidationRequest(validationRequestJson);
        if (accessValidationRequest == null)
        {
            _logger.LogWarning("Rejecting request as deserialization failed.");
            return httpRequest.CreateResponse(HttpStatusCode.Forbidden);
        }

        if (Guid.TryParse(httpRequest.Query["userId"], out var userId))
        {
            // Currently, only BFF is able to make these requests, so this constraint is valid.
            _logger.LogWarning("Rejecting request as userId was not provided.");
            return httpRequest.CreateResponse(HttpStatusCode.Forbidden);
        }

        var result = await _authorizationService
            .CreateSignatureAsync(accessValidationRequest, CancellationToken.None)
            .ConfigureAwait(false);

        await _revisionLogClient
            .LogAsync(new RevisionLogEntry(
                logId: Guid.NewGuid(),
                systemId: SubsystemInformation.Id,
                activity: "CreateSignature",
                occurredOn: SystemClock.Instance.GetCurrentInstant(),
                origin: nameof(AuthorizationHttpTrigger),
                userId: userId,
                payload: validationRequestJson))
            .ConfigureAwait(false);

        var response = httpRequest.CreateResponse(HttpStatusCode.OK);
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
