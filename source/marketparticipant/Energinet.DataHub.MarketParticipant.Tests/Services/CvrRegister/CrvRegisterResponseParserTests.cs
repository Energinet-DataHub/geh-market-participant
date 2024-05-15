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
using Energinet.DataHub.MarketParticipant.Infrastructure.Services.CvrRegister;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Services.CvrRegister;

[UnitTest]
public sealed class CrvRegisterResponseParserTests
{
    [Fact]
    public void GetValues_WhenResponseJsonIsValidAndPropertyIsOrganizationName_ReturnsOrganizationName()
    {
        // arrange
        const string validResponse = "{\"hits\":{\"total\":1,\"hits\":[{\"_source\":{\"Vrvirksomhed\":{\"virksomhedMetadata\":{\"nyesteNavn\":{\"navn\":\"TestVirksomhed\"}}}}}]}}";

        // act
        var actual = CrvRegisterResponseParser.GetValues<string>(validResponse, CvrRegisterProperty.OrganizationName).ToList();

        // assert
        Assert.Single(actual);
        Assert.Equal("TestVirksomhed", actual.Single());
    }

    [Fact]
    public void GetValues_WhenResponseJsonIsValidWithSeveralHits_ReturnsAllHits()
    {
        // arrange
        const string validResponse = "{\"hits\":{\"total\":2,\"hits\":[{\"_source\":{\"Vrvirksomhed\":{\"virksomhedMetadata\":{\"nyesteNavn\":{\"navn\":\"TestVirksomhed\"}}}}},{\"_source\":{\"Vrvirksomhed\":{\"virksomhedMetadata\":{\"nyesteNavn\":{\"navn\":\"TestVirksomhed2\"}}}}}]}}";

        // act
        var actual = CrvRegisterResponseParser.GetValues<string>(validResponse, CvrRegisterProperty.OrganizationName).ToList();

        // assert
        Assert.Equal(2, actual.Count);
        Assert.Equal("TestVirksomhed", actual[0]);
        Assert.Equal("TestVirksomhed2", actual[1]);
    }

    [Fact]
    public void GetValues_WhenResponseJsonIsValidWithNoHits_ReturnsEmptyList()
    {
        // arrange
        const string validResponse = "{\"hits\":{\"total\":0,\"hits\":[]}}";

        // act
        var actual = CrvRegisterResponseParser.GetValues<string>(validResponse, CvrRegisterProperty.OrganizationName).ToList();

        // assert
        Assert.Empty(actual);
    }
}
