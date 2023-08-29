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
using System.Linq;
using System.Reflection;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Moq;
using NodaTime;

namespace Energinet.DataHub.MarketParticipant.Tests.Common;

public static class MockedClock
{
    public static void Set(DateTime dateTime)
    {
        Set(() => Instant.FromDateTimeUtc(dateTime));
    }

    private static void Set(Func<Instant> clockFunction)
    {
        var field = typeof(Clock)
            .GetFields(BindingFlags.NonPublic | BindingFlags.Static)
            .Single();

        var mockedClock = new Mock<IClock>();
        mockedClock.Setup(x => x.GetCurrentInstant()).Returns(clockFunction);

        field.SetValue(null, mockedClock.Object);
    }
}
