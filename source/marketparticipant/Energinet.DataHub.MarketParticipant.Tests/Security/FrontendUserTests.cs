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
using Energinet.DataHub.MarketParticipant.Application.Security;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Security;

[UnitTest]
public sealed class FrontendUserTests
{
    [Theory]
    [InlineData(true, "332ABDA7-0E2D-4A17-B206-AAB0F3A48BC1", true)]
    [InlineData(true, "FED74717-E5BA-4C43-A29B-23F01F9B8F32", true)]
    [InlineData(false, "332ABDA7-0E2D-4A17-B206-AAB0F3A48BC1", true)]
    [InlineData(false, "FED74717-E5BA-4C43-A29B-23F01F9B8F32", false)]
    public void IsFasOrAssignedToOrganization_GivenInput_ReturnsExpectedValue(bool isFas, Guid organizationId, bool expected)
    {
        // Arrange
        var target = new FrontendUser(Guid.NewGuid(), organizationId, Guid.NewGuid(), isFas);

        // Act + Assert
        Assert.Equal(expected, target.IsFasOrAssignedToOrganization(Guid.Parse("332ABDA7-0E2D-4A17-B206-AAB0F3A48BC1")));
    }

    [Theory]
    [InlineData(true, "332ABDA7-0E2D-4A17-B206-AAB0F3A48BC1", true)]
    [InlineData(true, "FED74717-E5BA-4C43-A29B-23F01F9B8F32", true)]
    [InlineData(false, "332ABDA7-0E2D-4A17-B206-AAB0F3A48BC1", true)]
    [InlineData(false, "FED74717-E5BA-4C43-A29B-23F01F9B8F32", false)]
    public void IsFasOrAssignedToActor_GivenInput_ReturnsExpectedValue(bool isFas, Guid actorId, bool expected)
    {
        // Arrange
        var target = new FrontendUser(Guid.NewGuid(), Guid.NewGuid(), actorId, isFas);

        // Act + Assert
        Assert.Equal(expected, target.IsFasOrAssignedToActor(Guid.Parse("332ABDA7-0E2D-4A17-B206-AAB0F3A48BC1")));
    }

    [Theory]
    [InlineData(true, "332ABDA7-0E2D-4A17-B206-AAB0F3A48BC1", true)]
    [InlineData(true, "FED74717-E5BA-4C43-A29B-23F01F9B8F32", false)]
    [InlineData(false, "332ABDA7-0E2D-4A17-B206-AAB0F3A48BC1", true)]
    [InlineData(false, "FED74717-E5BA-4C43-A29B-23F01F9B8F32", false)]
    public void IsAssignedToActor_GivenInput_ReturnsExpectedValue(bool isFas, Guid actorId, bool expected)
    {
        // Arrange
        var target = new FrontendUser(Guid.NewGuid(), Guid.NewGuid(), actorId, isFas);

        // Act + Assert
        Assert.Equal(expected, target.IsAssignedToActor(Guid.Parse("332ABDA7-0E2D-4A17-B206-AAB0F3A48BC1")));
    }
}
