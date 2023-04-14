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
using System.Text.RegularExpressions;

namespace Energinet.DataHub.MarketParticipant.Domain.Model.Users.Authentication;

public sealed class SmsAuthenticationMethod : AuthenticationMethod
{
    public SmsAuthenticationMethod(PhoneNumber phoneNumber)
    {
        ArgumentNullException.ThrowIfNull(phoneNumber);

        PhoneNumber = phoneNumber;

        ValidatePhoneNumber();
    }

    public PhoneNumber PhoneNumber { get; }

    private void ValidatePhoneNumber()
    {
        if (!Regex.IsMatch(PhoneNumber.Number, "^\\+[0-9]+ [0-9]+$"))
        {
            throw new ValidationException("SMS authentication requires the phone number to be formatted as '+{country} {number}', e.g. +1 5555551234.");
        }
    }
}