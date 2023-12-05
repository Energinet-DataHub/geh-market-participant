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

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Microsoft.AspNetCore.Http;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Extensions;

public sealed class NotFoundValidationExceptionHandler : CommonExceptionHandlerBase<NotFoundValidationException>
{
    private static readonly ReadOnlyDictionary<string, object> _emptyArgs = new(new Dictionary<string, object>());
    private readonly string _errorCodePrefix;

    public NotFoundValidationExceptionHandler(string errorCodePrefix)
    {
        _errorCodePrefix = errorCodePrefix;
    }

    public override Task HandleExceptionAsync(NotFoundValidationException exception, HttpResponse response)
    {
        var errorDescriptor = new ErrorDescriptor(
            exception.Message,
            $"{_errorCodePrefix}.validation.not_found",
            NormalizeArgs(exception));

        response.StatusCode = StatusCodes.Status404NotFound;
        return response.WriteAsJsonAsync(new { errors = new[] { errorDescriptor } });
    }

    private static IReadOnlyDictionary<string, object> NormalizeArgs(ValidationException exception)
    {
        if (exception.Data[ValidationExceptionExtensions.ArgsDataKey] is IEnumerable<(string Key, object Value)> args)
            return args.ToDictionary(x => x.Key, x => x.Value);

        return _emptyArgs;
    }
}
