﻿// Copyright 2020 Energinet DataHub A/S
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
using System.Net.Mail;

namespace Energinet.DataHub.MarketParticipant.Domain.Model;

public sealed record EmailAddress
{
    public EmailAddress(string address)
    {
        Address = ValidateAddress(address);
    }

    public string Address { get; }

    public bool Equals(EmailAddress? other)
    {
        return string.Equals(Address, other?.Address, StringComparison.OrdinalIgnoreCase);
    }

    public override int GetHashCode()
    {
        return string.GetHashCode(Address, StringComparison.OrdinalIgnoreCase);
    }

    public override string ToString()
    {
        return Address;
    }

    private static string ValidateAddress(string address)
    {
        return !string.IsNullOrWhiteSpace(address) && address.Length <= 64 && MailAddress.TryCreate(address, out _)
            ? address.Trim()
            : throw new ValidationException($"The provided e-mail '{address}' is not valid.");
    }
}
