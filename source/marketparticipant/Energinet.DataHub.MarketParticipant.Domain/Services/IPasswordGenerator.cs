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

namespace Energinet.DataHub.MarketParticipant.Domain.Services;

/// <summary>
/// Generates random passwords.
/// </summary>
public interface IPasswordGenerator
{
    /// <summary>
    /// Generates a random password that satisfies the given complexity parameters.
    /// </summary>
    /// <param name="length">Lenght of the generated password.</param>
    /// <param name="characterSets">Character sets to include in the random generation of the password.</param>
    /// <param name="minNumberOfSetsToHit">How many of the specified characer sets must be used in the password.</param>
    string GenerateRandomPassword(int length, CharacterSet characterSets, int minNumberOfSetsToHit);
}
