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
public sealed class PasswordGeneratorTests
{
    [Fact]
    public void Generate_GeneratedPassword_MustMatchComplexityParams()
    {
        // arrange
        const int Length = 14;
        const CharacterSet Sets = CharacterSet.Numbers | CharacterSet.Lower | CharacterSet.Upper | CharacterSet.Special;
        const int SetsTohit = 4;

        var passwordChecker = new PasswordChecker();
        var target = new PasswordGenerator(passwordChecker);

        // act
        var actualPassword = target.GenerateRandomPassword(Length, Sets, SetsTohit);
        var actualComplexity = passwordChecker.PasswordSatisfiesComplexity(actualPassword, Length, Sets, SetsTohit);

        // assert
        Assert.True(actualComplexity);
    }
}
