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

using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Domain.Services;

[UnitTest]
public sealed class PasswordCheckerTests
{
    [Theory]
    [InlineData("5", CharacterSet.Lower, 1, false)]
    [InlineData("U", CharacterSet.Lower, 1, false)]
    [InlineData("L", CharacterSet.Lower, 1, false)]
    [InlineData("l", CharacterSet.Lower | CharacterSet.Upper, 1, true)]
    [InlineData("L", CharacterSet.Lower | CharacterSet.Upper, 1, true)]
    [InlineData("lL", CharacterSet.Lower | CharacterSet.Upper, 1, true)]
    [InlineData("lL", CharacterSet.Lower | CharacterSet.Upper, 2, true)]
    [InlineData("lL", CharacterSet.Lower | CharacterSet.Upper | CharacterSet.Special, 3, false)]
    [InlineData("lL@", CharacterSet.Lower | CharacterSet.Upper | CharacterSet.Special, 3, true)]
    public void PasswordSatisfiesComplexity_UsingSpecifiedCharacterSets_MatchesExpectedResult(string password, CharacterSet sets, int numberOfSetHitsRequired, bool expected)
    {
        // arrange
        var target = new PasswordChecker();

        // act
        var actual = target.PasswordSatisfiesComplexity(password, 1, sets, numberOfSetHitsRequired);

        // assert
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("longerthan8chars_!!@", 8, true)]
    [InlineData("exactly!", 8, true)]
    [InlineData("less!@7", 8, false)]
    public void PasswordSatisfiesComplexity_UsingSpecificedLengthParam_MatchesExpectedResult(string password, int minLenght, bool expected)
    {
        // arrange
        var target = new PasswordChecker();

        // act
        var actual = target.PasswordSatisfiesComplexity(password, minLenght, CharacterSet.Lower, 1);

        // assert
        Assert.Equal(expected, actual);
    }
}
