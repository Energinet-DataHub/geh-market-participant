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

using System.Collections.Generic;
using System.Linq;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Extensions;

public static class EnumerableExtensions
{
    public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
    {
        var skip = 0;
        var sourceList = source.ToList();

        while (true)
        {
            var segment = sourceList.Skip(skip).Take(batchSize).ToList();

            if (!segment.Any())
                yield break;

            yield return segment;

            if (segment.Count < batchSize)
                yield break;

            skip += segment.Count;
        }
    }
}
