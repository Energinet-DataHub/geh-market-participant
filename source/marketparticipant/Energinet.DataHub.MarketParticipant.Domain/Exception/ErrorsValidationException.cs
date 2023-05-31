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
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Energinet.DataHub.MarketParticipant.Domain.Exception
{
    public sealed class ErrorsValidationException : ValidationException
    {
        [Obsolete("Use ctor with a value.")]
        public ErrorsValidationException()
        {
        }

        public ErrorsValidationException(string message)
            : base(message)
        {
        }

        public ErrorsValidationException(string message, System.Exception innerException)
            : base(message, innerException)
        {
        }

        public ErrorsValidationException(string message, IEnumerable<(string Code, string Message)> errors)
            : base(message)
        {
            Errors = errors;
        }

        public ErrorsValidationException(string message, IEnumerable<(string Code, string Message)> errors, System.Exception innerException)
            : base(message, innerException)
        {
            Errors = errors;
        }

        public IEnumerable<(string Code, string Message)> Errors { get; } = Enumerable.Empty<(string Code, string Message)>();
    }
}
