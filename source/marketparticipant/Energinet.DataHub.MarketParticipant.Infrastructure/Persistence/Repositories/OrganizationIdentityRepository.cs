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
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Services.CvrRegister;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;

public sealed class OrganizationIdentityRepository : IOrganizationIdentityRepository
{
    private readonly IHttpClientFactory _httpClientFactory;

    public OrganizationIdentityRepository(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<OrganizationIdentity?> GetAsync(BusinessRegisterIdentifier businessRegisterIdentifier)
    {
        ArgumentNullException.ThrowIfNull(businessRegisterIdentifier);

        var client = _httpClientFactory.CreateClient("CvrRegister");

        var query = CvrRegisterRequestBuilder.Build(
            new CvrRegisterTermBusinessRegisterIdentifier(businessRegisterIdentifier.Identifier),
            CvrRegisterProperty.OrganizationName);

        using var content = new StringContent(query, Encoding.UTF8, "application/json");

        using var response = await client.PostAsync(
            new Uri("cvr-permanent/virksomhed/_search", UriKind.Relative),
            content).ConfigureAwait(false);

        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        var name = CrvRegisterResponseParser.GetValues<string>(json, CvrRegisterProperty.OrganizationName).SingleOrDefault();

        return name != null
            ? new OrganizationIdentity(name)
            : null;
    }
}
