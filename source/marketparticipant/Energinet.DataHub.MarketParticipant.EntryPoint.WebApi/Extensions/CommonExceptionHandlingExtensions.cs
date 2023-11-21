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
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using DataValidationException = System.ComponentModel.DataAnnotations.ValidationException;
using FluentValidationException = FluentValidation.ValidationException;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Extensions;

public static class CommonExceptionHandlingExtensions
{
    public static IApplicationBuilder UseCommonExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseExceptionHandler(appBuilder => appBuilder.Use(_ => HandleRequestExceptionAsync));
    }

    private static Task HandleRequestExceptionAsync(HttpContext context)
    {
        var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
        ArgumentNullException.ThrowIfNull(exceptionHandlerFeature, nameof(exceptionHandlerFeature));
        return HandleKnownExceptionAsync((dynamic)exceptionHandlerFeature.Error, context.Response);
    }

    private static Task HandleKnownExceptionAsync(NotFoundValidationException exception, HttpResponse response)
    {
        response.StatusCode = StatusCodes.Status404NotFound;
        return WriteErrorAsync(response, new ErrorDescriptor("NOT_FOUND_EXCEPTION", exception.Message));
    }

    private static Task HandleKnownExceptionAsync(FluentValidationException exception, HttpResponse response)
    {
        response.StatusCode = StatusCodes.Status400BadRequest;
        var affectedProperties = exception.Errors.Select(err => new ErrorDescriptor(err.ErrorCode, err.ErrorMessage, err.PropertyName));
        return WriteErrorAsync(response, new ErrorDescriptor("ARGUMENT_EXCEPTION", "See details property for more information.", Details: affectedProperties));
    }

    private static Task HandleKnownExceptionAsync(DataValidationException exception, HttpResponse response)
    {
        response.StatusCode = StatusCodes.Status400BadRequest;
        return WriteErrorAsync(response, new ErrorDescriptor("DOMAIN_EXCEPTION", exception.Message));
    }

    private static Task HandleKnownExceptionAsync(Exception exception, HttpResponse response)
    {
        response.StatusCode = StatusCodes.Status500InternalServerError;
        return WriteErrorAsync(response, new ErrorDescriptor("INTERNAL_ERROR", "An error occurred while processing the request."));
    }

    private static Task WriteErrorAsync(HttpResponse response, ErrorDescriptor error)
    {
        return response.WriteAsJsonAsync(new { error });
    }

    private sealed record ErrorDescriptor(
        string Code,
        string Message,
        string? Target = null,
        IEnumerable<ErrorDescriptor>? Details = null);
}
