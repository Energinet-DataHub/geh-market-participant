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
using Microsoft.AspNetCore.Http;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Extensions;

public sealed class FallbackExceptionHandler : CommonExceptionHandlerBase<Exception>
{
    private readonly string _errorCodePrefix;

    public FallbackExceptionHandler(string errorCodePrefix)
    {
        _errorCodePrefix = errorCodePrefix;
    }

    public override Task HandleExceptionAsync(Exception exception, HttpResponse response)
    {
        var errorDescriptor = new ErrorDescriptor(
            "An error occurred while processing the request.",
            $"{_errorCodePrefix}.internal_error",
            new Dictionary<string, object>());

        response.StatusCode = StatusCodes.Status500InternalServerError;
        return response.WriteAsJsonAsync(new { errors = new[] { errorDescriptor } });
    }
}
