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
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Extensions;

public sealed class FluentValidationExceptionHandler : CommonExceptionHandlerBase<ValidationException>
{
    private readonly string _errorCodePrefix;

    public FluentValidationExceptionHandler(string errorCodePrefix)
    {
        _errorCodePrefix = errorCodePrefix;
    }

    public override Task HandleExceptionAsync(ValidationException exception, HttpResponse response)
    {
        var errors = exception
            .Errors
            .Select(HandleValidationFailure);

        response.StatusCode = StatusCodes.Status400BadRequest;
        return response.WriteAsJsonAsync(new { errors });
    }

    private ErrorDescriptor HandleValidationFailure(ValidationFailure validationFailure)
    {
        var args = new Dictionary<string, object>
        {
            { "param", validationFailure.PropertyName },
            { "value", validationFailure.AttemptedValue }
        };

        switch (validationFailure.ErrorCode)
        {
            case "LengthValidator":
                {
                    args.Add("min", validationFailure.FormattedMessagePlaceholderValues["MinLength"]);
                    args.Add("max", validationFailure.FormattedMessagePlaceholderValues["MaxLength"]);
                    break;
                }
        }

        return new ErrorDescriptor(validationFailure.ErrorMessage, NormalizeErrorCode(validationFailure), args);
    }

    private string NormalizeErrorCode(ValidationFailure validationFailure)
    {
        var errorCode = validationFailure.ErrorCode switch
        {
            "NotEmptyValidator" => "missing_required_value",
            "NotNullValidator" => "missing_required_value",
            "LengthValidator" => "invalid_length",
            "GlobalLocationNumberValidation" => "invalid_gln",
            "EnergyIdentificationCodeValidation" => "invalid_gln",
            "StringEnumValidator" => "invalid_enum",
            _ => validationFailure.ErrorCode
        };

        return errorCode != null
            ? $"{_errorCodePrefix}.bad_argument.{errorCode}"
            : $"{_errorCodePrefix}.bad_argument";
    }
}
