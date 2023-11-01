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
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Azure.Security.KeyVault.Certificates;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MarketParticipant.Application.Services;

public class CertificateService : ICertificateService
{
    private readonly CertificateClient _keyVault;
    private readonly ILogger<CertificateService> _logger;

    public CertificateService(
        CertificateClient keyVault,
        ILogger<CertificateService> logger)
    {
        _keyVault = keyVault;
        _logger = logger;
    }

    public Task SaveCertificateAsync(string certificateLookupIdentifier, X509Certificate2 certificate)
    {
        ArgumentException.ThrowIfNullOrEmpty(certificateLookupIdentifier);
        ArgumentNullException.ThrowIfNull(certificate);

        return _keyVault.ImportCertificateAsync(new ImportCertificateOptions(certificateLookupIdentifier, certificate.RawData));
    }

    public X509Certificate2 CreateAndValidateX509Certificate(Stream certificate)
    {
        ArgumentNullException.ThrowIfNull(certificate);

        using var reader = new BinaryReader(certificate);
        var certificateBytes = reader.ReadBytes((int)certificate.Length);

        try
        {
            var x509Certificate2 = new X509Certificate2(certificateBytes);
            using var rsa = x509Certificate2.GetRSAPublicKey();
            return x509Certificate2;
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "Certificate validation failed");
            throw new ValidationException("Certificate invalid");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unknown Certificate validation exception");
            throw new ValidationException("Certificate invalid");
        }
    }
}
