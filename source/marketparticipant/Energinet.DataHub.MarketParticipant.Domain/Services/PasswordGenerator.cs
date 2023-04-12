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
using System.Security.Cryptography;

namespace Energinet.DataHub.MarketParticipant.Domain.Services;

public sealed class PasswordGenerator : IPasswordGenerator
{
    private readonly IPasswordChecker _passwordChecker;

    public PasswordGenerator(IPasswordChecker passwordChecker)
    {
        _passwordChecker = passwordChecker;
    }

    public string GenerateRandomPassword(int length, CharacterSet characterSets, int minNumberOfSetsToHit)
    {
        string password;
        do
        {
            password = new string(Enumerable.Range(0, length).Select(_ => (char)RandomNumberGenerator.GetInt32(32, 127)).ToArray());
        }
        while (!_passwordChecker.PasswordSatisfiesComplexity(password, length, characterSets, minNumberOfSetsToHit));

        return password;
    }
}
