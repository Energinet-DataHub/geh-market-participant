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
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Extensions;

public sealed class CommonExceptionHandlingBuilder
{
    private readonly List<(Type ExceptionType, Func<Exception, HttpResponse, Task> ExceptionHandler)> _exceptionHandlers = new();

    public void Use<T>(CommonExceptionHandlerBase<T> exceptionHandler)
        where T : Exception
    {
        ArgumentNullException.ThrowIfNull(exceptionHandler);
        _exceptionHandlers.Add((typeof(T), (ex, response) => exceptionHandler.HandleExceptionAsync((T)ex, response)));
    }

    public Task HandleRequestExceptionAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
        ArgumentNullException.ThrowIfNull(exceptionHandlerFeature);

        foreach (var (exceptionType, exceptionHandler) in _exceptionHandlers)
        {
            if (exceptionType.IsInstanceOfType(exceptionHandlerFeature.Error))
            {
                return exceptionHandler(exceptionHandlerFeature.Error, context.Response);
            }
        }

        return Task.CompletedTask;
    }
}
