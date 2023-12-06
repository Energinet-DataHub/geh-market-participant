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
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Azure;
using Azure.Security.KeyVault.Secrets;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MarketParticipant.Application.Services;

public class CertificateService : ICertificateService
{
    private readonly SecretClient _keyVault;
    private readonly ICertificateValidation _certificateValidation;
    private readonly ILogger<CertificateService> _logger;

    public CertificateService(
        SecretClient keyVault,
        ICertificateValidation certificateValidation,
        ILogger<CertificateService> logger)
    {
        _keyVault = keyVault;
        _certificateValidation = certificateValidation;
        _logger = logger;
    }

    public async Task SaveCertificateAsync(string certificateLookupIdentifier, X509Certificate2 certificate)
    {
        ArgumentException.ThrowIfNullOrEmpty(certificateLookupIdentifier);
        ArgumentNullException.ThrowIfNull(certificate);

        var convertedCertificateToBase64 = Convert.ToBase64String(certificate.RawData);
        try
        {
            await _keyVault
                .SetSecretAsync(new KeyVaultSecret(certificateLookupIdentifier, convertedCertificateToBase64))
                .ConfigureAwait(false);
        }
        catch (RequestFailedException e) when (e.Status == (int)HttpStatusCode.Conflict)
        {
            var opr = await _keyVault.StartRecoverDeletedSecretAsync(certificateLookupIdentifier).ConfigureAwait(false);
            await opr.WaitForCompletionAsync().ConfigureAwait(false);
        }
    }

    public async Task RemoveCertificateAsync(string certificateLookupIdentifier)
    {
        ArgumentException.ThrowIfNullOrEmpty(certificateLookupIdentifier);

        var opr = await _keyVault.StartDeleteSecretAsync(certificateLookupIdentifier).ConfigureAwait(false);
        await opr.WaitForCompletionAsync().ConfigureAwait(false);
    }

    public X509Certificate2 CreateAndValidateX509Certificate(Stream certificate)
    {
        ArgumentNullException.ThrowIfNull(certificate);

        using var reader = new BinaryReader(certificate);
        var certificateBytes = reader.ReadBytes((int)certificate.Length);

        try
        {
            var x509Certificate2 = new X509Certificate2(certificateBytes);
            _certificateValidation.Verify(x509Certificate2);
            return x509Certificate2;
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, $"Certificate validation failed: {ex.InnerException}");
            throw new ValidationException("Certificate validation failed.")
                .WithErrorCode("actor.credentials.invalid");
        }
    }
}
