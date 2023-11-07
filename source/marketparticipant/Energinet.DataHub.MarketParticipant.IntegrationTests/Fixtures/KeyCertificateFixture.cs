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
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Energinet.DataHub.Core.FunctionApp.TestCommon;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;

public sealed class KeyCertificateFixture : IAsyncLifetime
{
    public string CertificateName { get; } = $"IntegrationTestCertificateKey-{Guid.NewGuid()}";
    public SecretClient CertificateClient { get; private set; } = null!;

    public Task InitializeAsync()
    {
        var keyVaultUri = GetKeyVaultUri();
        CertificateClient = new SecretClient(keyVaultUri, new DefaultAzureCredential());

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await CleanUpCertificateFromStorageAsync(CertificateName);
    }

    public async Task CleanUpCertificateFromStorageAsync(string lookupName)
    {
        try
        {
            await CertificateClient.StartDeleteSecretAsync(CertificateName);
        }
        catch (RequestFailedException requestFailedException) when (requestFailedException.Status == 404)
        {
            await Task.CompletedTask;
        }
    }

    public async Task<string> GetPublicKeyTestCertificateAsync(string certificateName)
    {
        var resourceName = $"Energinet.DataHub.MarketParticipant.IntegrationTests.Common.Certificates.{certificateName}";
        var assembly = typeof(KeyCertificateFixture).Assembly;
        var stream = assembly.GetManifestResourceStream(resourceName);

        using var reader = new BinaryReader(stream!);
        var certificateBytes = reader.ReadBytes((int)stream!.Length);

        using var certificate = new X509Certificate2(certificateBytes);
        var convertedCertificateToBase64 = Convert.ToBase64String(certificate.RawData);
        await CertificateClient.SetSecretAsync(certificateName, convertedCertificateToBase64);

        return certificate.Thumbprint;
    }

    private static Uri GetKeyVaultUri()
    {
        var integrationTestConfiguration = new ConfigurationBuilder()
            .AddJsonFile("integrationtest.local.settings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        return new Uri(integrationTestConfiguration.GetValue("AZURE_KEYVAULT_URL"));
    }
}
