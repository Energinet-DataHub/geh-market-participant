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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Authorization.Infrastructure.Persistence.Model;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Energinet.DataHub.MarketParticipant.Authorization.Infrastructure.Persistence.Repositories;

public sealed class DownloadTokenRespository : IDownloadTokenRespository
{
    private readonly IMarketParticipantDbContext _marketParticipantDbContext;

    public DownloadTokenRespository(IMarketParticipantDbContext marketParticipantDbContext)
    {
        _marketParticipantDbContext = marketParticipantDbContext;
    }

    public async Task<Guid> CreateDownloadTokenAsync(string authorization)
    {
        ArgumentNullException.ThrowIfNull(authorization);

        var downloadTokenEntity = new DownloadTokenEntity
        {
            Authorization = authorization,
            Created = SystemClock.Instance.GetCurrentInstant().ToDateTimeOffset(),
        };

        _marketParticipantDbContext
            .DownloadTokens
            .Add(downloadTokenEntity);

        await _marketParticipantDbContext.SaveChangesAsync().ConfigureAwait(false);

        return downloadTokenEntity.Token;
    }

    public async Task<string> ExchangeDownloadTokenAsync(Guid downloadToken)
    {
        ArgumentNullException.ThrowIfNull(downloadToken);
        var onlyValidForFiveMinutes = SystemClock.Instance
                                .GetCurrentInstant()
                                .Minus(Duration.FromMinutes(5))
                                .ToDateTimeOffset();

        var downloadTokenEntity = await _marketParticipantDbContext
            .DownloadTokens
                .FirstOrDefaultAsync(
                    x => x.Used == false &&
                    x.Token == downloadToken &&
                    x.Created > onlyValidForFiveMinutes)
            .ConfigureAwait(false);

        if (downloadTokenEntity == null)
            return string.Empty;

        var authorization = downloadTokenEntity.Authorization;

        downloadTokenEntity.Used = true;
        downloadTokenEntity.Authorization = string.Empty;

        await _marketParticipantDbContext.SaveChangesAsync().ConfigureAwait(false);

        return authorization;
    }
}
