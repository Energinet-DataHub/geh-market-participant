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
using System.Globalization;
using Energinet.DataHub.MarketParticipant.Domain.Exception;

namespace Energinet.DataHub.MarketParticipant.Domain.Model;

public sealed record Address
{
    public Address(
        string? streetName,
        string? number,
        string? zipCode,
        string? city,
        string country)
    {
        EnsureValidCountry(country);

        StreetName = streetName;
        Number = number;
        ZipCode = zipCode;
        City = city;
        Country = country;
    }

    public string? StreetName { get; }
    public string? Number { get; }
    public string? ZipCode { get; }
    public string? City { get; }
    public string Country { get; }

    private static void EnsureValidCountry([NotNull] string? country)
    {
        ArgumentNullException.ThrowIfNull(country);

        try
        {
            if (country.Length == 2)
            {
                _ = new RegionInfo(country);
            }
            else
            {
                throw new ArgumentException(country);
            }
        }
        catch (ArgumentException)
        {
            throw new ValidationException($"The specified country '{country}' is not supported.")
                .WithErrorCode("organization.country.invalid")
                .WithArgs(("country", country));
        }
    }
}
