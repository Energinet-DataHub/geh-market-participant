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

using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using Energinet.DataHub.MarketParticipant.Authorization.Model;
using Energinet.DataHub.MarketParticipant.Authorization.Model.AccessValidationVerify;
using Energinet.DataHub.MarketParticipant.Authorization.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MarketParticipant.Authorization.Http;

public sealed class EndpointAuthorizationContext : IEndpointAuthorizationContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IVerifyAuthorization _verifyAuthorization;
    private readonly IEndpointAuthorizationLogger _endpointAuthorizationLogger;
    private readonly ILogger<EndpointAuthorizationContext> _logger;

    public EndpointAuthorizationContext(
        IHttpContextAccessor httpContextAccessor,
        IVerifyAuthorization verifyAuthorization,
        IEndpointAuthorizationLogger endpointAuthorizationLogger,
        ILogger<EndpointAuthorizationContext> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _verifyAuthorization = verifyAuthorization;
        _endpointAuthorizationLogger = endpointAuthorizationLogger;
        _logger = logger;
    }

    public async Task<AuthorizationResult> VerifyAsync(AccessValidationVerifyRequest verifyRequest)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            throw new InvalidOperationException("HttpContext required for endpoint authorization.");
        }

        AuthorizationResult authorizationResult;

        if (TryReadSignature(httpContext, out var signature))
        {
            var result = await _verifyAuthorization
                .VerifySignatureAsync(verifyRequest, signature)
                .ConfigureAwait(false);

            authorizationResult = result
                ? new AuthorizationSuccess(signature.RequestId)
                : new AuthorizationFailure(signature.RequestId);
        }
        else
        {
            authorizationResult = httpContext.Request.Headers.ContainsKey(EndpointAuthorizationConfig.AuthorizationHeaderName)
                ? new AuthorizationFailure()
                : new AuthorizationUnavailable();
        }

        await _endpointAuthorizationLogger
            .LogAsync(verifyRequest, authorizationResult)
            .ConfigureAwait(false);

        return authorizationResult;
    }

    private bool TryReadSignature(HttpContext httpContext, [NotNullWhen(true)] out Signature? signature)
    {
        if (!httpContext.Request.Headers.TryGetValue(EndpointAuthorizationConfig.AuthorizationHeaderName, out var headers)
            || headers.Count != 1
            || headers[0] == null)
        {
            signature = null;
            return false;
        }

        _logger.LogWarning("Read Base64 signature from header: {Header}", headers[0]!);
        var signatureHeader = headers[0]!;
        signatureHeader = signatureHeader.PadRight(signatureHeader.Length + (signatureHeader.Length % 4), '=');
        var signatureBase64 = Convert.FromBase64String(signatureHeader);
        var signatureJson = Encoding.UTF8.GetString(signatureBase64);
        signature = JsonSerializer.Deserialize<Signature>(signatureJson);
        return signature != null;
    }
}
