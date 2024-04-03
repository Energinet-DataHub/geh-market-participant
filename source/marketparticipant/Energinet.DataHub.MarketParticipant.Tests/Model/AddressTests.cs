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
public sealed class AddressTests
{
    [Theory]
    [InlineData("", false)]
    [InlineData("da", false)]
    [InlineData("da-dk", false)]
    [InlineData("dk", true)]
    [InlineData("DK", true)]
    [InlineData("SE", true)]
    [InlineData("NO", true)]
    [InlineData("FI", true)]
    [InlineData("GB", true)]
    [InlineData("DE", true)]
    public void Ctor_Country_IsValidated(string country, bool isAllowed)
    {
        if (isAllowed)
        {
            var address = new Address(null, null, null, null, country);
            Assert.NotNull(address);
        }
        else
        {
            Assert.Throws<ValidationException>(() => new Address(null, null, null, null, country));
        }
    }
}
