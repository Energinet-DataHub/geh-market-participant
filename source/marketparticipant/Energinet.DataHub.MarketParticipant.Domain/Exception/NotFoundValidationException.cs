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
using System.Diagnostics.CodeAnalysis;

namespace Energinet.DataHub.MarketParticipant.Domain.Exception
{
    public sealed class NotFoundValidationException : ValidationException
    {
        [Obsolete("Use ctor with a value.")]
        public NotFoundValidationException()
            : base("Entity does not exist.")
        {
        }

        [Obsolete("Use ctor with a value.")]
        public NotFoundValidationException(string message)
            : base(message)
        {
        }

        [Obsolete("Use ctor with a value.")]
        public NotFoundValidationException(string message, System.Exception innerException)
            : base(message, innerException)
        {
        }

        public NotFoundValidationException(Guid id)
            : this(id, CreateMessage(id))
        {
        }

        public NotFoundValidationException(Guid id, string message)
            : base(message)
        {
            this.WithArgs(("id", id));
        }

        public static void ThrowIfNull([NotNull] object? value, Guid id)
        {
            if (value == null)
            {
                throw new NotFoundValidationException(id);
            }
        }

        public static void ThrowIfNull([NotNull] object? value, Guid id, string message)
        {
            if (value == null)
            {
                throw new NotFoundValidationException(id, message);
            }
        }

        private static string CreateMessage(Guid id)
        {
            return $"Entity '{id}' does not exist.";
        }
    }
}
