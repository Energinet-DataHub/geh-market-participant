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
    public sealed class CertificateServiceTests
    {
        private readonly CertificateFixture _certificateFixture;

        public CertificateServiceTests(CertificateFixture certificateFixture)
        {
            _certificateFixture = certificateFixture;
        }

        [Fact]
        public void CertificateService_CreateAndValidate_Invalid()
        {
            // Arrange
            var certificateService = new CertificateService(
                _certificateFixture.SecretClient,
                new CertificateValidation(),
                new Mock<ILogger<CertificateService>>().Object);

            using var memoryStream = new MemoryStream();
            memoryStream.Write("Invalid certificate integrations tests"u8);

            // Act + Assert
            Assert.Throws<ValidationException>(() => certificateService.CreateAndValidateX509Certificate(memoryStream));
        }

        [Fact]
        public void CertificateService_CreateAndValidate_Valid()
        {
            // Arrange
            var validationMock = new Mock<ICertificateValidation>();

            var certificateService = new CertificateService(
                _certificateFixture.SecretClient,
                validationMock.Object,
                new Mock<ILogger<CertificateService>>().Object);

            using var fileStream = SetupTestCertificate("integration-actor-test-certificate-public.cer");

            // Act
            var certificate = certificateService.CreateAndValidateX509Certificate(fileStream);

            // Assert
            Assert.NotNull(certificate);
            validationMock.Verify(x => x.Verify(certificate), Times.Once);
        }

        [Fact]
        public async Task CertificateService_Save()
        {
            // Arrange
            var certificateService = new CertificateService(
                _certificateFixture.SecretClient,
                new Mock<ICertificateValidation>().Object,
                new Mock<ILogger<CertificateService>>().Object);

            var name = Guid.NewGuid().ToString();

            await using var fileStream = SetupTestCertificate("integration-actor-test-certificate-public.cer");

            var x509Certificate = certificateService.CreateAndValidateX509Certificate(fileStream);

            try
            {
                // Act
                await certificateService.SaveCertificateAsync(name, x509Certificate);

                // Assert
                Assert.True(await _certificateFixture.CertificateExistsAsync(name));
            }
            finally
            {
                await _certificateFixture.CleanUpCertificateFromStorageAsync(name);
            }
        }

        private static Stream SetupTestCertificate(string certificateName)
        {
            var resourceName = $"Energinet.DataHub.MarketParticipant.IntegrationTests.Common.Certificates.{certificateName}";

            var assembly = typeof(CertificateServiceTests).Assembly;
            var stream = assembly.GetManifestResourceStream(resourceName);

            return stream ?? throw new InvalidOperationException($"Could not find resource {resourceName}");
        }
    }
}
