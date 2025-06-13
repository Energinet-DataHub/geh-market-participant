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

using System.Globalization;
using Energinet.DataHub.MarketParticipant.Domain.Model;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Common;

public sealed class MockedMeteringPointIdentifier
{
    private static long _no = 100000000000000000;
    private readonly string _value;

    public MockedMeteringPointIdentifier()
    {
        ++_no;
        _value = _no.ToString(CultureInfo.InvariantCulture);
    }

#pragma warning disable CA1062, CA2225, CA5394
    public static implicit operator MeteringPointIdentification(MockedMeteringPointIdentifier mock)
    {
        return new MeteringPointIdentification(mock._value);
    }
#pragma warning restore

    public static MeteringPointIdentification New()
    {
        return new MockedMeteringPointIdentifier();
    }
}
