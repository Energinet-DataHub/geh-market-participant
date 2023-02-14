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

namespace Energinet.DataHub.MarketParticipant.Domain.Repositories;

public sealed record Result<T, TErrorType>
    where TErrorType : struct
{
    private readonly T? _value;

    public Result(TErrorType error)
    {
        Error = error;
    }

    public Result(T value)
    {
        _value = value;
    }

    public TErrorType? Error { get; }

    public T Value => _value ?? throw new InvalidOperationException("Value has not been initialized, please check the Error property.");

    public void ThrowOnError(Func<TErrorType, System.Exception> errorHandler)
    {
        ArgumentNullException.ThrowIfNull(errorHandler);

        if (Error != null)
        {
            throw errorHandler(Error.Value);
        }
    }
}
