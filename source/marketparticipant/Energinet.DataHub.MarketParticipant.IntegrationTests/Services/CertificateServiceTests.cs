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
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Services
{
    [Collection(nameof(IntegrationTestCollectionFixture))]
    [IntegrationTest]
    public sealed class CertificateServiceTests : IClassFixture<KeyCertificateFixture>
    {
        private readonly KeyCertificateFixture _keyCertificateFixture;

        public CertificateServiceTests(KeyCertificateFixture keyCertificateFixture)
        {
            _keyCertificateFixture = keyCertificateFixture;
        }

        [Fact]
        public void CertificateService_CreateAndValidate_Invalid()
        {
            // Arrange
            var certificateService = new CertificateService(_keyCertificateFixture.CertificateClient, new Mock<ILogger<CertificateService>>().Object);
            using var memoryStream = new MemoryStream();
            memoryStream.Write(Encoding.UTF8.GetBytes("Invalid certificate"));

            // Act + Assert
            Assert.Throws<ValidationException>(() => certificateService.CreateAndValidateX509Certificate(memoryStream));
        }

        [Fact]
        public async Task CertificateService_CreateAndValidate_Valid()
        {
            // Arrange
            var certificateService = new CertificateService(_keyCertificateFixture.CertificateClient, new Mock<ILogger<CertificateService>>().Object);
            await using var fileStream = SetupTestCertificate("integration-actor-test-certificate-public.cer");

            // Act
            var certificate = certificateService.CreateAndValidateX509Certificate(fileStream);

            // Assert
            Assert.NotNull(certificate);
            Assert.True(certificate.Verify());
        }

        [Fact]
        public async Task CertificateService_Save()
        {
            // Arrange
            var certificateService = new CertificateService(_keyCertificateFixture.CertificateClient, new Mock<ILogger<CertificateService>>().Object);

            await using var fileStream = SetupTestCertificate("integration-actor-test-certificate-public.cer");

            var x509Certificate = certificateService.CreateAndValidateX509Certificate(fileStream);

            // Act
            await certificateService.SaveCertificateAsync(_keyCertificateFixture.CertificateName, x509Certificate);

            // Assert
            var savedCertificate = await _keyCertificateFixture.CertificateClient.GetSecretAsync(_keyCertificateFixture.CertificateName);
            Assert.True(savedCertificate.HasValue);
            Assert.Equal(_keyCertificateFixture.CertificateName, savedCertificate.Value.Name);
        }

        private static Stream SetupTestCertificate(string certificateName)
        {
            var resourceName = $"Energinet.DataHub.MarketParticipant.IntegrationTests.Common.Certificates.{certificateName}";

            var assembly = typeof(CertificateServiceTests).Assembly;
            var stream = assembly.GetManifestResourceStream(resourceName);

            SaveCertificateToStore(certificateName);

            return stream ?? throw new InvalidOperationException($"Could not find resource {resourceName}");
        }

        private static void SaveCertificateToStore(string certificateName)
        {
            var resourceName = $"Energinet.DataHub.MarketParticipant.IntegrationTests.Common.Certificates.{certificateName}";

            var assembly = typeof(CertificateServiceTests).Assembly;
            using var fileStream = assembly.GetManifestResourceStream(resourceName);

            using var reader = new BinaryReader(fileStream);
            var certificateBytes = reader.ReadBytes((int)fileStream.Length);

            using var certificate = new X509Certificate2(certificateBytes);
            using var certificateStore = new X509Store(StoreName.My, StoreLocation.CurrentUser, OpenFlags.ReadWrite);

            var searchResult = certificateStore.Certificates.Find(X509FindType.FindByThumbprint, "02ba07db5e00130b0a008c1c9283552408701c58", false);

            if (searchResult.Count <= 0)
            {
                certificateStore.Add(certificate);
                certificateStore.Close();
            }
        }
    }
}
