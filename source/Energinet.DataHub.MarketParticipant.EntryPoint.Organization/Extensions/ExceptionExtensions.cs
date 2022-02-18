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
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Energinet.DataHub.MarketParticipant.EntryPoint.Organization.Model;
using Energinet.DataHub.MarketParticipant.Utilities;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using DataAnnotationException = System.ComponentModel.DataAnnotations.ValidationException;
using FluentValidationException = FluentValidation.ValidationException;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.Organization.Extensions
{
    // TODO: This is a copy from PO, we need to streamline this.
    public static class ExceptionExtensions
    {
        public static void Log(this Exception source, ILogger logger)
        {
            Guard.ThrowIfNull(source, nameof(source));

            if (source is not FluentValidationException or DataAnnotationException)
            {
                logger.LogError(source, "An error occurred while processing request");

                // Observed that LogError does not always write the exception.
                logger.LogError(source.ToString());
            }
        }

        public static HttpResponseData AsHttpResponseData(this Exception source, HttpRequestData request)
        {
            static HttpResponseData CreateHttpResponseData(HttpRequestData request, HttpStatusCode httpStatusCode, ErrorDescriptor error)
            {
                static MemoryStream JsonSerialize(ErrorDescriptor error)
                {
                    var bytes = JsonSerializer.SerializeToUtf8Bytes(
                        new ErrorResponse(error),
                        new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                        });
                    return new MemoryStream(bytes);
                }

                var stream = JsonSerialize(error);

                return request.CreateResponse(stream, httpStatusCode);
            }

            Guard.ThrowIfNull(source, nameof(source));
            Guard.ThrowIfNull(request, nameof(request));

            return source switch
            {
                FluentValidationException ve =>
                    CreateHttpResponseData(
                        request,
                        HttpStatusCode.BadRequest,
                        new ErrorDescriptor(
                            "VALIDATION_EXCEPTION",
                            "See details",
                            details: ve.Errors.Select(x =>
                                new ErrorDescriptor(
                                    x.ErrorCode,
                                    x.ErrorMessage,
                                    x.PropertyName)))),

                DataAnnotationException ve =>
                    CreateHttpResponseData(
                        request,
                        HttpStatusCode.BadRequest,
                        new ErrorDescriptor(
                            "VALIDATION_EXCEPTION",
                            ve.Message)),

                _ =>
                    CreateHttpResponseData(
                        request,
                        HttpStatusCode.InternalServerError,
                        new ErrorDescriptor(
                            "INTERNAL_ERROR",
                            "An error occured while processing the request."))
            };
        }
    }
}
