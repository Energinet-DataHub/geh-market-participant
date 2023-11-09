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
using System.Threading.Tasks;
using Azure.Security.KeyVault.Secrets;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.CertificateSynchronization.Services;

public sealed class KeyVaultCertificates : IKeyVaultCertificates
{
    private readonly SecretClient _secretClient;

    public KeyVaultCertificates(SecretClient secretClient)
    {
        _secretClient = secretClient;
    }

    public async Task<IReadOnlyCollection<CertificateIdentifier>> GetCertificateIdentifiersAsync()
    {
        var certificates = new List<CertificateIdentifier>();

        await foreach (var certificateSecret in _secretClient
                           .GetPropertiesOfSecretsAsync()
                           .ConfigureAwait(false))
        {
            certificates.Add(new CertificateIdentifier(
                certificateSecret.Id,
                certificateSecret.Name,
                false));
        }

        await foreach (var deletedCertificateSecret in _secretClient
                           .GetDeletedSecretsAsync()
                           .ConfigureAwait(false))
        {
            certificates.Add(new CertificateIdentifier(
                deletedCertificateSecret.Id,
                deletedCertificateSecret.Name,
                true));
        }

        return certificates;
    }

    public Task PurgeDeletedCertificateAsync(CertificateIdentifier certificate)
    {
        ArgumentNullException.ThrowIfNull(certificate);

        if (!certificate.IsDeleted)
            throw new ArgumentException("Certificate must be deleted before it can be purged.", nameof(certificate));

        return _secretClient.PurgeDeletedSecretAsync(certificate.Name);
    }
}
