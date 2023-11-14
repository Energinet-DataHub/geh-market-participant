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
using Energinet.DataHub.MarketParticipant.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;

public sealed class CertificateFixture : IAsyncLifetime
{
    public SecretClient SecretClient { get; private set; } = null!;
    public CertificateService CertificateService { get; private set; } = null!;

    public Task InitializeAsync()
    {
        var keyVaultUri = GetKeyVaultUri();
        SecretClient = new SecretClient(keyVaultUri, new DefaultAzureCredential());

        var certValidation = new Mock<ICertificateValidation>();

        CertificateService = new CertificateService(
            SecretClient,
            certValidation.Object,
            new Mock<ILogger<CertificateService>>().Object);

        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task CleanUpCertificateFromStorageAsync(string name)
    {
        try
        {
            var opr = await CertificateClient.StartDeleteSecretAsync(CertificateName);
            await opr.WaitForCompletionAsync();
        }
        catch (RequestFailedException requestFailedException) when (requestFailedException.Status == 404)
        {
            await Task.CompletedTask;
        }
    }

    public async Task<bool> CertificateExistsAsync(string name)
    {
        try
        {
            var secret = await SecretClient.GetSecretAsync(name);
            return secret.HasValue;
        }
        catch (RequestFailedException requestFailedException) when (requestFailedException.Status == 404)
        {
            return false;
        }
    }

    public async Task<X509Certificate2> CreatePublicKeyCertificateAsync(string name)
    {
        var resourceName = "Energinet.DataHub.MarketParticipant.IntegrationTests.Common.Certificates.integration-actor-test-certificate-public.cer";
        var assembly = typeof(CertificateFixture).Assembly;
        var stream = assembly.GetManifestResourceStream(resourceName);

        using var reader = new BinaryReader(stream!);
        var certificateBytes = reader.ReadBytes((int)stream!.Length);

        var certificate = new X509Certificate2(certificateBytes);

        var convertedCertificateToBase64 = Convert.ToBase64String(certificate.RawData);
        await SecretClient.SetSecretAsync(name, convertedCertificateToBase64);

        return certificate;
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
