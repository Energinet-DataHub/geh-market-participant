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
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using NodaTime.Extensions;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Services;

// TODO: Tests
public sealed class ActorCertificateService : IActorCertificateService
{
    private readonly IClock _clock;
    private readonly IMarketParticipantDbContext _marketParticipantDbContext;

    public ActorCertificateService(
        IClock clock,
        IMarketParticipantDbContext marketParticipantDbContext)
    {
        _clock = clock;
        _marketParticipantDbContext = marketParticipantDbContext;
    }

    public async Task<Instant> CalculateExpirationDateAsync(X509Certificate2 certificate)
    {
        ArgumentNullException.ThrowIfNull(certificate);

        var certificateAddedAt = await FindCertificateAddedDateAsync(certificate.Thumbprint).ConfigureAwait(false);

        var businessExpiresOn = certificateAddedAt.Plus(Duration.FromDays(365));
        var certificateExpiresOn = certificate.NotAfter.ToUniversalTime().ToInstant();
        return Instant.Min(businessExpiresOn, certificateExpiresOn);
    }

    private async Task<Instant> FindCertificateAddedDateAsync(string certificateThumbprint)
    {
        var existing = await _marketParticipantDbContext
            .UsedActorCertificates
            .FirstOrDefaultAsync(usedCert => usedCert.Thumbprint == certificateThumbprint)
            .ConfigureAwait(false);

        return existing?.AddedAt.ToInstant() ?? _clock.GetCurrentInstant();
    }
}
