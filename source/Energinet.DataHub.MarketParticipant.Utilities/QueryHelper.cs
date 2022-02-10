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
using System.ComponentModel;
using System.Globalization;

namespace Energinet.DataHub.MarketParticipant.Utilities
{
    public static class QueryHelper
    {
        private static T GetFromQueryString<T>(Uri uri)
            where T : new()
        {
            var parsedQueryString = System.Web.HttpUtility.ParseQueryString(uri.Query);

            var obj = new T();
            var properties = typeof(T).GetProperties();

            foreach (var property in properties)
            {
                var valueAsString = parsedQueryString.Get(property.Name);

                if (string.IsNullOrWhiteSpace(valueAsString))
                {
                    continue;
                }

                var value = Parse(valueAsString, property.PropertyType);



                property.SetValue(obj, value, null);
            }

            return obj;
        }

        private static object? Parse(string valueToConvert, Type dataType)
        {
            TypeConverter obj = TypeDescriptor.GetConverter(dataType);
            var value = obj.ConvertFromString(null, CultureInfo.InvariantCulture,  valueToConvert);
            return value;
        }
    }
}
