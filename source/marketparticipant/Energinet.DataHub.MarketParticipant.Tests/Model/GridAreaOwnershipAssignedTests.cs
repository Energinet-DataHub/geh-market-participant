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
using Energinet.DataHub.MarketParticipant.Domain.Model.Events;
using Energinet.DataHub.MarketParticipant.Tests.Common;
using NodaTime;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Model;

[UnitTest]
public sealed class GridAreaOwnershipAssignedTests
{
    [Fact]
    public void Ctor_ValidFrom_StartsAtNextDay()
    {
        // Arrange
        var actorNumber = new MockedGln();
        var gridAreaId = new GridAreaId(Guid.NewGuid());

        MockedClock.Set(new DateTime(2023, 08, 29, 23, 0, 0, DateTimeKind.Utc));

        // Act
        var gridAreaOwnershipAssigned = new GridAreaOwnershipAssigned(actorNumber, EicFunction.GridAccessProvider, gridAreaId);

        // Assert
        Assert.Equal(new DateTime(2023, 08, 30, 22, 00, 00, DateTimeKind.Utc), gridAreaOwnershipAssigned.ValidFrom.ToDateTimeUtc());
    }
}
