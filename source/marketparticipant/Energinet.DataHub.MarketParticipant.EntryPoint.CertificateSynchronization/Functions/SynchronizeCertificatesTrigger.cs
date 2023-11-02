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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.EntryPoint.CertificateSynchronization.Services;
using Microsoft.Azure.Functions.Worker;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.CertificateSynchronization.Functions;

public sealed class SynchronizeCertificatesTrigger
{
    private readonly IKeyVaultCertificates _keyVaultCertificates;
    private readonly IApimCertificateStore _apimCertificateStore;

    public SynchronizeCertificatesTrigger(
        IKeyVaultCertificates keyVaultCertificates,
        IApimCertificateStore apimCertificateStore)
    {
        _keyVaultCertificates = keyVaultCertificates;
        _apimCertificateStore = apimCertificateStore;
    }

    [Function(nameof(SynchronizeCertificatesTrigger))]
    public async Task RunAsync([TimerTrigger("* */1 * * *", RunOnStartup = true)] FunctionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var keyVaultCertificates = await _keyVaultCertificates
            .GetCertificateIdentifiersAsync()
            .ConfigureAwait(false);

        var apimCertificates = await _apimCertificateStore
            .GetCertificateIdentifiersAsync()
            .ConfigureAwait(false);

        await AddNewCertificatesAsync(keyVaultCertificates, apimCertificates).ConfigureAwait(false);
        await RemoveOldCertificatesAsync(keyVaultCertificates, apimCertificates).ConfigureAwait(false);
    }

    private async Task AddNewCertificatesAsync(
        IEnumerable<CertificateIdentifier> certificatesInKeyVault,
        IReadOnlyCollection<CertificateIdentifier> certificatesInApim)
    {
        var certificatesToAdd = certificatesInKeyVault
            .Where(c => !c.IsDeleted)
            .Except(certificatesInApim);

        foreach (var certificateIdentifier in certificatesToAdd)
        {
            if (certificatesInApim.Contains(certificateIdentifier with { IsDeleted = true }))
            {
                await _apimCertificateStore
                    .RemoveCertificateAsync(certificateIdentifier)
                    .ConfigureAwait(false);
            }

            await _apimCertificateStore
                .AddCertificateAsync(certificateIdentifier)
                .ConfigureAwait(false);
        }
    }

    private async Task RemoveOldCertificatesAsync(
        IEnumerable<CertificateIdentifier> certificatesInKeyVault,
        IEnumerable<CertificateIdentifier> certificatesInApim)
    {
        foreach (var certificateIdentifier in certificatesInKeyVault.Where(c => c.IsDeleted))
        {
            await _apimCertificateStore
                .RemoveCertificateAsync(certificateIdentifier)
                .ConfigureAwait(false);

            await _keyVaultCertificates
                .PurgeDeletedCertificateAsync(certificateIdentifier)
                .ConfigureAwait(false);
        }

        foreach (var certificateIdentifier in certificatesInApim.Where(c => c.IsDeleted))
        {
            await _apimCertificateStore
                .RemoveCertificateAsync(certificateIdentifier)
                .ConfigureAwait(false);
        }
    }
}
