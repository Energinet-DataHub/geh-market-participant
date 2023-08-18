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

namespace Energinet.DataHub.MarketParticipant.Application.Extensions;

public static class EnumerableExtensions
{
    public static IEnumerable<(TLeft? Left, TRight? Right)> FullOuterJoin<TLeft, TRight>(
        IEnumerable<TLeft> leftItems,
        IEnumerable<TRight> rightItems,
        Func<TLeft, TRight, bool> equality)
    {
        ArgumentNullException.ThrowIfNull(leftItems);
        ArgumentNullException.ThrowIfNull(rightItems);
        ArgumentNullException.ThrowIfNull(equality);

        var remainingRightItems = rightItems.ToHashSet();

        foreach (var leftItem in leftItems)
        {
            var hasMatch = false;

            foreach (var rightItem in remainingRightItems)
            {
                var isMatched = equality(leftItem, rightItem);
                if (!isMatched)
                    continue;

                yield return (leftItem, rightItem);

                remainingRightItems.Remove(rightItem);
                hasMatch = true;
                break;
            }

            if (!hasMatch)
            {
                yield return (leftItem, default);
            }
        }

        foreach (var rightItem in remainingRightItems)
        {
            yield return (default, rightItem);
        }
    }
}
