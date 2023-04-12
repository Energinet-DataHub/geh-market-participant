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

using System.Linq;
using Energinet.DataHub.MarketParticipant.Infrastructure.Model.Permissions;
using NodaTime;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Model;

[UnitTest]
public sealed class KnownPermissionsTests
{
    [Fact]
    public void All_Permissions_AreUnique()
    {
        // Arrange + Act + Assert
        Assert.Equal(KnownPermissions.All.Count, KnownPermissions.All.Select(p => p.Id).Distinct().Count());
    }

    [Fact]
    public void All_Claims_AreUnique()
    {
        // Arrange + Act + Assert
        Assert.Equal(KnownPermissions.All.Count, KnownPermissions.All.Select(p => p.Claim).Distinct().Count());
    }

    [Fact]
    public void All_Created_AreInThePast()
    {
        // Arrange + Act + Assert
        Assert.True(KnownPermissions.All.All(p => p.Created < SystemClock.Instance.GetCurrentInstant()));
    }

    [Fact]
    public void All_AssignableTo_HaveItems()
    {
        // Arrange + Act + Assert
        Assert.True(KnownPermissions.All.All(p => p.AssignableTo.Any()));
    }
}
