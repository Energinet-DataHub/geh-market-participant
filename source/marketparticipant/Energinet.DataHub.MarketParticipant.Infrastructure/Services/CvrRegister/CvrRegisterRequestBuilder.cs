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
using System.Text.Json;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Services.CvrRegister;

public static class CvrRegisterRequestBuilder
{
    public static string Build<T>(T term, params CvrRegisterProperty[] properties)
        where T : ICvrRegisterTerm
    {
        var request = new
        {
            _source = properties.Select(x => x.Value),
            query = new
            {
                @bool = new
                {
                    must = new[]
                    {
                        new
                        {
                            term,
                        },
                    },
                },
            },
        };
        return JsonSerializer.Serialize(request);
    }
}
