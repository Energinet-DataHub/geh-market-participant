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

using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using System.Text.RegularExpressions;
using Energinet.DataHub.MarketParticipant.Domain.Exception;

namespace Energinet.DataHub.MarketParticipant.Domain.Model;

public sealed class OrganizationDomain
{
    public OrganizationDomain(string value)
    {
        if (!IsValid(value))
        {
            throw new ValidationException($"The specified value '{value}' is not a valid domain.")
                .WithErrorCode("organization.domain.invalid")
                .WithArgs(("value", value));
        }

        Value = value;
    }

    public string Value { get; }

    public static bool IsValid(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var topLevelDomainSeparatorIndex = value.LastIndexOf('.');

        return
            value.Length < 65 &&
            topLevelDomainSeparatorIndex > 0 &&
            topLevelDomainSeparatorIndex < value.Length - 2 &&
            Regex.IsMatch(value, "^[A-Za-z0-9]{1}[A-Za-z0-9\\-.]+$") &&
            MailAddress.TryCreate("noreply@" + value, out _);
    }
}
