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
using System.ComponentModel.DataAnnotations;

namespace Energinet.DataHub.MarketParticipant.Domain.Exception;

public static class ValidationExceptionExtensions
{
    public const string ErrorCodeDataKey = "errorCode";
    public const string ArgsDataKey = "args";

    public static ValidationException WithErrorCode(this ValidationException exception, string errorCode)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(errorCode);

        exception.Data[ErrorCodeDataKey] = errorCode;
        return exception;
    }

    public static ValidationException WithArgs(this ValidationException exception, params (string Key, object Value)[] args)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(args);

        exception.Data[ArgsDataKey] = args;
        return exception;
    }
}
