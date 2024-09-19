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
using System.Globalization;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using NodaTime;
using NodaTime.Extensions;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;

public sealed class B2CLogRepository : IB2CLogRepository
{
    private readonly GraphServiceClient _graphClient;

    public B2CLogRepository(GraphServiceClient graphClient)
    {
        _graphClient = graphClient;
    }

    public async IAsyncEnumerable<B2CLoginAttemptLogEntry> GetLoginAttempsAsync(Instant cutoff)
    {
        var response = await GetAsync(cutoff).ConfigureAwait(false);

        while (response?.Value?.Count > 0)
        {
            foreach (var signIn in response.Value)
            {
                var errorCode = signIn.Status!.ErrorCode!.Value;

                yield return new B2CLoginAttemptLogEntry(
                    signIn.Id!,
                    signIn.CreatedDateTime!.Value.ToInstant(),
                    signIn.IpAddress!,
                    signIn.Location!.CountryOrRegion!,
                    signIn.UserId!,
                    signIn.UserPrincipalName!,
                    signIn.ResourceId!,
                    signIn.ResourceDisplayName!,
                    errorCode,
                    errorCode != 0 ? signIn.Status!.FailureReason : null);
            }

            response = await GetNextPageAsync(response).ConfigureAwait(false);
        }
    }

    private Task<SignInCollectionResponse?> GetAsync(Instant cutoff)
    {
        return _graphClient.AuditLogs
            .SignIns
            .GetAsync(x => x.QueryParameters.Filter = $"createdDateTime gt {cutoff.ToString("g", CultureInfo.InvariantCulture)}");
    }

    private Task<SignInCollectionResponse?> GetNextPageAsync(SignInCollectionResponse previousResponse)
    {
        if (previousResponse.OdataNextLink == null)
        {
            return Task.FromResult<SignInCollectionResponse?>(null);
        }

        return _graphClient.AuditLogs
            .SignIns
            .WithUrl(previousResponse.OdataNextLink)
            .GetAsync();
    }
}
