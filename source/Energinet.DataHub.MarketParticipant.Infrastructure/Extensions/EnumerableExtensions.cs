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
using System.Reflection;

namespace Energinet.DataHub.MarketParticipant.Infrastructure;

public static class EnumerableExtensions
{
    public static IOrderedEnumerable<T> OrderBy<T>(this IEnumerable<T> source, string property)
    {
        ArgumentNullException.ThrowIfNull(source);
        var propertyInfo = GetPropertyInfo(source, property);
        return source.OrderBy(x => propertyInfo.GetValue(x));
    }

    public static IOrderedEnumerable<T> OrderByDescending<T>(this IEnumerable<T> source, string property)
    {
        ArgumentNullException.ThrowIfNull(source);
        var propertyInfo = GetPropertyInfo(source, property);
        return source.OrderByDescending(x => propertyInfo.GetValue(x));
    }

    private static PropertyInfo GetPropertyInfo<T>(IEnumerable<T> source, string property)
    {
        var propertyInfo = typeof(T).GetProperty(property);

        if (propertyInfo == null)
        {
            throw new ArgumentException($"Property not found. Available properties are: {string.Join(", ", typeof(T).GetProperties().Select(x => x.Name))}");
        }

        return propertyInfo;
    }
}
