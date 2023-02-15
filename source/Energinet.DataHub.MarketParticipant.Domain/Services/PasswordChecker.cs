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
using System.Collections.Generic;
using System.Linq;

namespace Energinet.DataHub.MarketParticipant.Domain.Services;

public sealed class PasswordChecker : IPasswordChecker
{
    private Dictionary<CharacterSet, HashSet<char>> _sets = new()
    {
        { CharacterSet.Numbers, Enumerable.Range(48, 10).Select(x => (char)x).ToHashSet() },
        { CharacterSet.Lower, Enumerable.Range(97, 26).Select(x => (char)x).ToHashSet() },
        { CharacterSet.Upper, Enumerable.Range(65, 26).Select(x => (char)x).ToHashSet() },
        {
            CharacterSet.Special, Enumerable.Range(32, 16)
                .Concat(Enumerable.Range(58, 7)
                    .Concat(Enumerable.Range(91, 6)
                        .Concat(Enumerable.Range(123, 4)))).Select(x => (char)x).ToHashSet()
        },
    };

    public bool PasswordSatisfiesComplexity(string password, int minLength, CharacterSet characterSets, int minNumberOfSetsToHit)
    {
        ArgumentNullException.ThrowIfNull(password);

        if (minLength < 1)
        {
            throw new InvalidOperationException($"{nameof(minLength)} must be greather than 0");
        }

        var charSets = _sets.Where(x => (characterSets & x.Key) == x.Key).Select(x => x.Value).ToArray();

        if (minNumberOfSetsToHit < 0 || minNumberOfSetsToHit > charSets.Length)
        {
            throw new InvalidOperationException($"{nameof(minNumberOfSetsToHit)} must be greater than or equal to 0 and less than or equal to the number of {nameof(characterSets)} specified.");
        }

        if (password.Length < minLength)
        {
            return false;
        }

        var hits = new bool[charSets.Length];

        foreach (var c in password)
        {
            for (var i = 0; i < charSets.Length; ++i)
            {
                if (charSets[i].Contains(c))
                {
                    hits[i] = true;
                    break;
                }
            }
        }

        return hits.Count(x => x) >= minNumberOfSetsToHit;
    }
}
