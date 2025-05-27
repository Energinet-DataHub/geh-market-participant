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
using Energinet.DataHub.MarketParticipant.Domain.Model;

namespace Energinet.DataHub.MarketParticipant.Tests.Common;

#pragma warning disable CA1062, CA2225

internal sealed class RandomlyGeneratedEmailAddress
{
    private readonly string _value = $"{Guid.NewGuid()}@test.datahub.dk";

    public static implicit operator string(RandomlyGeneratedEmailAddress mock)
    {
        return mock._value;
    }

    public static implicit operator EmailAddress(RandomlyGeneratedEmailAddress mock)
    {
        return new EmailAddress(mock._value);
    }

    public override string ToString()
    {
        return _value;
    }
}

#pragma warning restore
