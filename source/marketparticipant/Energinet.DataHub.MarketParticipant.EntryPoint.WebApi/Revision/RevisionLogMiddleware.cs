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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.IdentityModel.JsonWebTokens;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Revision;

public sealed class RevisionLogMiddleware : IMiddleware
{
    private static readonly Guid _currentSystemIdentifier = Guid.Parse("DA19142E-D419-4ED2-9798-CE5546260F84");
    private static readonly JsonSerializerOptions _jsonSerializerOptions
        = new JsonSerializerOptions().ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);

    private readonly IRevisionActivityPublisher _revisionActivityPublisher;

    public RevisionLogMiddleware(IRevisionActivityPublisher revisionActivityPublisher)
    {
        _revisionActivityPublisher = revisionActivityPublisher;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var endpoint = context.GetEndpoint();
        if (endpoint == null)
        {
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            return;
        }

        var revisionAttribute = endpoint.Metadata.GetMetadata<EnableRevisionAttribute>();
        if (revisionAttribute == null)
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        var payload = await ReadPayloadAsync(context).ConfigureAwait(false);
        var revisionLogEntry = CreateRevisionLogEntry(context, revisionAttribute, payload);

        await _revisionActivityPublisher
            .PublishAsync(revisionLogEntry)
            .ConfigureAwait(false);

        await next(context).ConfigureAwait(false);
    }

    private static string CreateRevisionLogEntry(
        HttpContext context,
        EnableRevisionAttribute revisionAttribute,
        string payload)
    {
        var message = new RevisionLogEntryDto(
            Guid.NewGuid(),
            GetUserId(context.User.Claims),
            GetActorId(context.User.Claims),
            _currentSystemIdentifier,
            SystemClock.Instance.GetCurrentInstant(),
            revisionAttribute.ActivityName,
            context.Request.GetEncodedPathAndQuery(),
            payload,
            revisionAttribute.EntityType.Name,
            revisionAttribute.LookupEntityKey(context.Request));

        return JsonSerializer.Serialize(message, _jsonSerializerOptions);
    }

    private static async Task<string> ReadPayloadAsync(HttpContext context)
    {
        var httpRequest = context.Request;
        var payload = "[no json body]";

        if (!httpRequest.HasJsonContentType())
        {
            return payload;
        }

        httpRequest.EnableBuffering();

        using var streamReader = new StreamReader(httpRequest.Body, leaveOpen: true);
        payload = await streamReader
            .ReadToEndAsync()
            .ConfigureAwait(false);

        httpRequest.Body.Position = 0;
        return payload;
    }

    private static Guid GetUserId(IEnumerable<Claim> claims)
    {
        // The use of 'ClaimTypes.NameIdentifier' is explained here: https://github.com/AzureAD/azure-activedirectory-identitymodel-extensions-for-dotnet/issues/415
        var userId = claims.Single(claim => claim.Type == ClaimTypes.NameIdentifier).Value;
        return Guid.Parse(userId);
    }

    private static Guid GetActorId(IEnumerable<Claim> claims)
    {
        var actorId = claims.Single(claim => claim.Type == JwtRegisteredClaimNames.Azp).Value;
        return Guid.Parse(actorId);
    }
}
