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
using System.Text.Json.Nodes;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Services.CvrRegister;

public static class CrvRegisterResponseParser
{
    public static IEnumerable<T> GetValues<T>(string response, CvrRegisterProperty cvrRegisterProperty)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(cvrRegisterProperty);

        var path = cvrRegisterProperty.Value.Split(".", StringSplitOptions.RemoveEmptyEntries);
        var json = JsonNode.Parse(response);
        var hits = json?["hits"]?["total"]?.GetValue<int>() ?? 0;

        for (var i = 0; i < hits; ++i)
        {
            var source = json?["hits"]?["hits"]?[i]?["_source"];

            foreach (var part in path)
            {
                if (source == null) break;
                source = source[part];
            }

            if (source == null) continue;

            var val = source.GetValue<T>();

            if (val != null)
            {
                yield return val;
            }
        }
    }
}
