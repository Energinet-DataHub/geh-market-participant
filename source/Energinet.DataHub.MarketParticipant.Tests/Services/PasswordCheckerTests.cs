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
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Domain.Services;

[UnitTest]
public sealed class PasswordCheckerTests
{
    private const string AllVisibleAsciiChars = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ !\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~";

    [Theory]

    // matches all conditions
    [InlineData("G00dLongPassw!", 14, CharacterSet.Numbers | CharacterSet.Lower | CharacterSet.Upper | CharacterSet.Special, 4, true)]

    // no special chars
    [InlineData("G00dLongPassw0", 14, CharacterSet.Numbers | CharacterSet.Lower | CharacterSet.Upper | CharacterSet.Special, 4, false)]

    // no numbers
    [InlineData("GoodLongPassw!", 14, CharacterSet.Numbers | CharacterSet.Lower | CharacterSet.Upper | CharacterSet.Special, 4, false)]

    // no upper case
    [InlineData("g00dlongpassw!", 14, CharacterSet.Numbers | CharacterSet.Lower | CharacterSet.Upper | CharacterSet.Special, 4, false)]

    // not long enough
    [InlineData("G00dLngPassw!", 14, CharacterSet.Numbers | CharacterSet.Lower | CharacterSet.Upper | CharacterSet.Special, 4, false)]
    public void PasswordSatisfiesComplexity_PasswordMustSatisfyAllCondition(string password, int minLenght, CharacterSet sets, int numberOfSetHitsRequired, bool expected)
    {
        // arrange
        var target = new PasswordChecker();

        // act
        var actual = target.PasswordSatisfiesComplexity(password, minLenght, sets, numberOfSetHitsRequired);

        // assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void PasswordSatisfiesComplexity_AllVisibleAsciiChars_Allowed()
    {
        // arrange
        var target = new PasswordChecker();

        // act
        var actual = target.PasswordSatisfiesComplexity(AllVisibleAsciiChars, 1, CharacterSet.Numbers | CharacterSet.Lower | CharacterSet.Upper | CharacterSet.Special, 4);

        // assert
        Assert.True(actual);
    }

    [Fact]
    public void PasswordSatisfiesComplexity_NonAsciiChars_Throws()
    {
        // arrange
        var target = new PasswordChecker();

        // act, assert
        Assert.Equal(
            "Invalid character (Parameter 'password')",
            Assert.Throws<ArgumentException>(() => target.PasswordSatisfiesComplexity(AllVisibleAsciiChars + "ø", 1, CharacterSet.Numbers | CharacterSet.Lower | CharacterSet.Upper | CharacterSet.Special, 4)).Message);
    }
}
