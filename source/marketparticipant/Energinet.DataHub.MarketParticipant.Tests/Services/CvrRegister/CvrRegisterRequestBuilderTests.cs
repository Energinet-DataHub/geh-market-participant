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

using Energinet.DataHub.MarketParticipant.Infrastructure.Services.CvrRegister;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Services.CvrRegister;

[UnitTest]
public sealed class CvrRegisterRequestBuilderTests
{
    [Fact]
    public void Build_WhenCalledWithTermAndProperties_ReturnsSerializedRequest()
    {
        // arrange
        var term = new CvrRegisterTermBusinessRegisterIdentifier("12345678");
        var property = CvrRegisterProperty.OrganizationName;
        var expected = $"{{\"_source\":[\"{property.Value}\"],\"query\":{{\"bool\":{{\"must\":[{{\"term\":{{\"Vrvirksomhed.cvrNummer\":\"{term.Value}\"}}}}]}}}}}}";

        // act
        var actual = CvrRegisterRequestBuilder.Build(term, property);

        // assert
        Assert.Equal(expected, actual);
    }
}
