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
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Services.CvrRegister;

public static class CrvRegisterResponseParser
{
    public static IEnumerable<T> GetValues<T>(string responseJson, CvrRegisterProperty cvrRegisterProperty)
    {
        ArgumentNullException.ThrowIfNull(responseJson);
        ArgumentNullException.ThrowIfNull(cvrRegisterProperty);

        var path = cvrRegisterProperty.Value.Split(".", StringSplitOptions.RemoveEmptyEntries);
        var response = JsonSerializer.Deserialize<CvrResponse>(responseJson);

        for (var i = 0; i < response!.Hits.Total; ++i)
        {
            var source = response.Hits.Hits[i].Source;

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

    // ReSharper disable ClassNeverInstantiated.Local
    // ReSharper disable UnusedAutoPropertyAccessor.Local
    // ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
    private sealed record CvrResponseHit
    {
        [JsonPropertyName("_source")]
        public JsonNode? Source { get; set; }
    }

    private sealed record CvrResponseHits
    {
        [JsonPropertyName("hits")]
        public CvrResponseHit[] Hits { get; set; } = Array.Empty<CvrResponseHit>();

        [JsonPropertyName("total")]
        public int Total { get; set; }
    }

    private sealed record CvrResponse
    {
        [JsonPropertyName("hits")]
        public CvrResponseHits Hits { get; set; } = null!;
    }

    // ReSharper restore AutoPropertyCanBeMadeGetOnly.Local
    // ReSharper restore UnusedAutoPropertyAccessor.Local
    // ReSharper restore ClassNeverInstantiated.Local
}
