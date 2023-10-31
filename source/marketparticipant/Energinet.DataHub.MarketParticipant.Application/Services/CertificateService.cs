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
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Azure.Security.KeyVault.Certificates;
using Energinet.DataHub.MarketParticipant.Domain.Model;

namespace Energinet.DataHub.MarketParticipant.Application.Services;

public class CertificateService : ICertificateService
{
    private readonly CertificateClient _keyVault;

    public CertificateService(CertificateClient keyVault)
    {
        _keyVault = keyVault;
    }

    public async Task<ActorCertificateCredentials> AddCertificateToKeyVaultAsync(string certificateName, Stream certificate)
    {
        ArgumentException.ThrowIfNullOrEmpty(certificateName);
        ArgumentNullException.ThrowIfNull(certificate);

        using var reader = new BinaryReader(certificate);
        var certificateBytes = reader.ReadBytes((int)certificate.Length);

        var response = await _keyVault.ImportCertificateAsync(new ImportCertificateOptions(certificateName, certificateBytes)).ConfigureAwait(false);

        var thumbprint = Encoding.UTF8.GetString(response.Value.Properties.X509Thumbprint);

        return new ActorCertificateCredentials(thumbprint, response.Value.SecretId.ToString());
    }
}
