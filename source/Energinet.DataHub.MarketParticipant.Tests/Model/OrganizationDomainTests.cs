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

using System.ComponentModel.DataAnnotations;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Model;

[UnitTest]
public sealed class OrganizationDomainTests
{
    [Theory]
    [InlineData("energinet", false)]
    [InlineData("energinet.com", true)]
    [InlineData("energinet.dk", true)]
    [InlineData("energinet.d", false)]
    [InlineData("datahub.energinet.com", true)]
    [InlineData("datahub.energinet.dk", true)]
    [InlineData("datahub.energinet.d", false)]
    [InlineData("/energinet.dk", false)]
    [InlineData("-energinet.dk", false)]
    [InlineData("https://energinet.dk", false)]
    [InlineData(null, false)]
    [InlineData("", false)]
    public void IsValid_InvalidOrValidDomain_MatchesExpected(string domain, bool expected)
    {
        // arrange
        var target = OrganizationDomain.IsValid;

        // act
        var actual = target(domain);

        // assert
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("energinet")]
    [InlineData("energinet.d")]
    [InlineData("datahub.energinet.d")]
    [InlineData("/energinet.dk")]
    [InlineData("-energinet.dk")]
    [InlineData("https://energinet.dk")]
    [InlineData(null)]
    [InlineData("")]
    public void Ctor_InvalidDomain_Throws(string domain)
    {
        // arrange, act, assert
        Assert.Throws<ValidationException>(() => new OrganizationDomain(domain));
    }

    [Fact]
    public void Ctor_ValidDomain_CreatesValidDomainObject()
    {
        // arrange
        const string expected = "energinet.dk";

        // act
        var actual = new OrganizationDomain(expected);

        // assert
        Assert.Equal(expected, actual.Value);
    }
}
