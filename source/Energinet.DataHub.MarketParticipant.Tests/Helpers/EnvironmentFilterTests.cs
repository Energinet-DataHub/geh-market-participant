// // Copyright 2020 Energinet DataHub A/S
// //
// // Licensed under the Apache License, Version 2.0 (the "License2");
// // you may not use this file except in compliance with the License.
// // You may obtain a copy of the License at
// //
// //     http://www.apache.org/licenses/LICENSE-2.0
// //
// // Unless required by applicable law or agreed to in writing, software
// // distributed under the License is distributed on an "AS IS" BASIS,
// // WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// // See the License for the specific language governing permissions and
// // limitations under the License.
using System;
using Energinet.DataHub.MarketParticipant.ApplyDBMigrationsApp.Helpers;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Helpers;

[UnitTest]
public class EnvironmentFilterTests
{
    [Theory]
    [InlineData(".Scripts.U_002.Seed.test1.sql", new string[] { "includeSeedData", "U-002" }, true)]
    [InlineData(".Scripts.U_002.Model.test1.sql", new string[] { "", "U-002" }, true)]
    [InlineData(".Scripts.U_001.Seed.test1.sql", new string[] { "includeSeedData", "U-002" }, false)]
    [InlineData(".Scripts.U_002.Seed.test1.sql", new string[] { "", "U-002" }, false)]
    [InlineData(".Scripts.U_002.Seed.test1.sq2l", new string[] { "", "U-002" }, false)]
    [InlineData(".Scripts.Seed.test1.sql", new string[] { "includeSeedData", "" }, true)]
    public void EnvironmentFilter_CorrectlyValidatesInput(string compareStrings, string[] args, bool isValid)
    {
        // Arrange
        var filter = EnvironmentFilter.GetFilter(args);

        // Act+Assert
        if (isValid)
        {
            Assert.True(filter(compareStrings));
        }
        else
        {
            Assert.False(filter(compareStrings));
        }
    }

    [Fact]
    public void EnvironmentFilter_NullInput_ThrowsException()
    {
        // Arrange+Act+Assert
        Assert.Throws<ArgumentNullException>(() => EnvironmentFilter.GetFilter(null));
    }
}
