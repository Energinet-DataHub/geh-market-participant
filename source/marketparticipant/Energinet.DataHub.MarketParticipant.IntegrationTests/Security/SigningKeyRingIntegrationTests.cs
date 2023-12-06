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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Security;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Moq;
using NodaTime;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Security;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class SigningKeyRingIntegrationTests : IClassFixture<KeyClientFixture>
{
    private readonly KeyClientFixture _keyClientFixture;
    private readonly DateTimeOffset _pastTime = DateTimeOffset.UtcNow.AddYears(-1);
    private readonly DateTimeOffset _futureTime = DateTimeOffset.UtcNow.AddYears(1);

    public SigningKeyRingIntegrationTests(KeyClientFixture keyClientFixture)
    {
        _keyClientFixture = keyClientFixture;
    }

    [Fact]
    public async Task GetKeysAsync_HasTestKey_ReturnsKey()
    {
        // Arrange
        var target = new SigningKeyRing(
            SystemClock.Instance,
            _keyClientFixture.KeyClient,
            _keyClientFixture.KeyName);

        // Act
        var keys = await target.GetKeysAsync();

        // Assert
        var jwk = keys.Single();
        Assert.Equal(_keyClientFixture.KeyName + "/", new Uri(jwk.Id).Segments[^2]);
    }

    [Fact]
    public async Task GetKeysAsync_ExceptionOccurs_DoesNotDeadlock()
    {
        // Arrange
        var keyClient = new Mock<KeyClient>();
        keyClient.Setup(x => x.GetPropertiesOfKeyVersionsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Throws(() => new InvalidOperationException());

        var target = new SigningKeyRing(
            SystemClock.Instance,
            keyClient.Object,
            _keyClientFixture.KeyName);

        // Act, Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => target.GetKeysAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() => target.GetKeysAsync());
    }

    [Fact]
    public async Task GetKeysAsync_Twice_ReturnsFromCache()
    {
        // Arrange
        var target = new SigningKeyRing(
            SystemClock.Instance,
            _keyClientFixture.KeyClient,
            _keyClientFixture.KeyName);

        var initialKeys = await target.GetKeysAsync();

        // Act
        var keys = await target.GetKeysAsync();

        // Assert
        Assert.Equal(initialKeys.Single(), keys.Single());
    }

    [Fact]
    public async Task GetKeysAsync_WithDisabledKey_ReturnsNothing()
    {
        // Arrange
        var target = new SigningKeyRing(
            SystemClock.Instance,
            _keyClientFixture.KeyClient,
            _keyClientFixture.KeyName);

        await _keyClientFixture
            .KeyClient
            .UpdateKeyPropertiesAsync(new KeyProperties(_keyClientFixture.KeyName)
            {
                Enabled = false
            });

        // Act
        var keys = await target.GetKeysAsync();

        // Assert
        Assert.Empty(keys);

        await _keyClientFixture
            .KeyClient
            .UpdateKeyPropertiesAsync(new KeyProperties(_keyClientFixture.KeyName)
            {
                Enabled = true
            });
    }

    [Fact]
    public async Task GetKeysAsync_WithExpiredKey_ReturnsNothing()
    {
        // Arrange
        var target = new SigningKeyRing(
            SystemClock.Instance,
            _keyClientFixture.KeyClient,
            _keyClientFixture.KeyName);

        await _keyClientFixture
            .KeyClient
            .UpdateKeyPropertiesAsync(new KeyProperties(_keyClientFixture.KeyName)
            {
                ExpiresOn = _pastTime,
                NotBefore = _pastTime.AddDays(-1)
            });

        // Act
        var keys = await target.GetKeysAsync();

        // Assert
        Assert.Empty(keys);

        // Cleanup
        await _keyClientFixture
            .KeyClient
            .UpdateKeyPropertiesAsync(new KeyProperties(_keyClientFixture.KeyName)
            {
                ExpiresOn = DateTimeOffset.UtcNow.AddYears(2),
                NotBefore = _pastTime
            });
    }

    [Fact]
    public async Task GetKeysAsync_AndSigningClient_ReturnsSameKey()
    {
        // Arrange
        var futureClock = new Mock<IClock>();
        futureClock
            .Setup(clock => clock.GetCurrentInstant())
            .Returns(Instant.FromDateTimeOffset(_futureTime));

        var target = new SigningKeyRing(
            futureClock.Object,
            _keyClientFixture.KeyClient,
            _keyClientFixture.KeyName);

        var signingClient = await target.GetSigningClientAsync();

        // Act
        var keys = await target.GetKeysAsync();

        // Assert
        Assert.Single(keys, x => x.Id == signingClient.KeyId);
    }

    [Fact]
    public async Task GetSigningClientAsync_GivenData_SignsData()
    {
        // Arrange
        var futureClock = new Mock<IClock>();
        futureClock
            .Setup(clock => clock.GetCurrentInstant())
            .Returns(Instant.FromDateTimeOffset(_futureTime));

        var target = new SigningKeyRing(
            futureClock.Object,
            _keyClientFixture.KeyClient,
            _keyClientFixture.KeyName);

        var data = new byte[] { 1, 2, 3 };

        // Act
        var signingClient = await target.GetSigningClientAsync();
        var signature = await signingClient.SignDataAsync(SignatureAlgorithm.RS256, data);

        // Assert
        Assert.NotNull(signature);
        Assert.NotEmpty(signature.Signature);
    }

    [Fact]
    public async Task GetSigningClientAsync_KeyNewerThan10Minutes_NoKey()
    {
        // Arrange
        var target = new SigningKeyRing(
            SystemClock.Instance,
            _keyClientFixture.KeyClient,
            _keyClientFixture.KeyName);

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(() => target.GetSigningClientAsync());
    }

    [Fact]
    public async Task GetSigningClientAsync_KeyExpired_NoKey()
    {
        // Arrange
        var futureClock = new Mock<IClock>();
        futureClock
            .Setup(clock => clock.GetCurrentInstant())
            .Returns(Instant.FromDateTimeOffset(DateTimeOffset.UtcNow.AddDays(-1)));

        var target = new SigningKeyRing(
            futureClock.Object,
            _keyClientFixture.KeyClient,
            _keyClientFixture.KeyName);

        await _keyClientFixture
            .KeyClient
            .UpdateKeyPropertiesAsync(new KeyProperties(_keyClientFixture.KeyName)
            {
                ExpiresOn = _pastTime,
                NotBefore = _pastTime.AddDays(-1)
            });

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => target.GetSigningClientAsync());

        // Cleanup
        await _keyClientFixture
            .KeyClient
            .UpdateKeyPropertiesAsync(new KeyProperties(_keyClientFixture.KeyName)
            {
                ExpiresOn = DateTimeOffset.UtcNow.AddYears(2),
                NotBefore = _pastTime
            });
    }

    [Fact]
    public async Task GetSigningClientAsync_KeyExpires_ReturnsKey()
    {
        // Arrange
        var futureClock = new Mock<IClock>();
        futureClock
            .Setup(clock => clock.GetCurrentInstant())
            .Returns(Instant.FromDateTimeOffset(DateTimeOffset.UtcNow.AddDays(1)));

        var target = new SigningKeyRing(
            futureClock.Object,
            _keyClientFixture.KeyClient,
            _keyClientFixture.KeyName);

        await _keyClientFixture
            .KeyClient
            .UpdateKeyPropertiesAsync(new KeyProperties(_keyClientFixture.KeyName)
            {
                ExpiresOn = DateTimeOffset.UtcNow.AddDays(3),
                NotBefore = _pastTime
            });

        // Act
        var signingClient = await target.GetSigningClientAsync();

        // Assert
        Assert.NotNull(signingClient);

        // Cleanup
        await _keyClientFixture
            .KeyClient
            .UpdateKeyPropertiesAsync(new KeyProperties(_keyClientFixture.KeyName)
            {
                ExpiresOn = DateTimeOffset.UtcNow.AddYears(2),
                NotBefore = _pastTime
            });
    }

    [Fact]
    public async Task GetSigningClientAsync_NotBefore_NoKey()
    {
        // Arrange
        var futureClock = new Mock<IClock>();
        futureClock
            .Setup(clock => clock.GetCurrentInstant())
            .Returns(Instant.FromDateTimeOffset(DateTimeOffset.UtcNow.AddDays(-1)));

        var target = new SigningKeyRing(
            futureClock.Object,
            _keyClientFixture.KeyClient,
            _keyClientFixture.KeyName);

        await _keyClientFixture
            .KeyClient
            .UpdateKeyPropertiesAsync(new KeyProperties(_keyClientFixture.KeyName)
            {
                ExpiresOn = _futureTime,
                NotBefore = DateTimeOffset.UtcNow.AddDays(1)
            });

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => target.GetSigningClientAsync());

        // Cleanup
        await _keyClientFixture
            .KeyClient
            .UpdateKeyPropertiesAsync(new KeyProperties(_keyClientFixture.KeyName)
            {
                ExpiresOn = _futureTime,
                NotBefore = DateTimeOffset.UtcNow.AddYears(-2)
            });
    }

    [Fact]
    public async Task GetSigningClientAsync_PastNotBefore_ReturnsKey()
    {
        // Arrange
        var futureClock = new Mock<IClock>();
        futureClock
            .Setup(clock => clock.GetCurrentInstant())
            .Returns(Instant.FromDateTimeOffset(DateTimeOffset.UtcNow.AddDays(1)));

        var target = new SigningKeyRing(
            futureClock.Object,
            _keyClientFixture.KeyClient,
            _keyClientFixture.KeyName);

        await _keyClientFixture
            .KeyClient
            .UpdateKeyPropertiesAsync(new KeyProperties(_keyClientFixture.KeyName)
            {
                ExpiresOn = _futureTime,
                NotBefore = DateTimeOffset.UtcNow.AddDays(-2)
            });

        // Act
        var signingClient = await target.GetSigningClientAsync();

        // Assert
        Assert.NotNull(signingClient);

        // Cleanup
        await _keyClientFixture
            .KeyClient
            .UpdateKeyPropertiesAsync(new KeyProperties(_keyClientFixture.KeyName)
            {
                ExpiresOn = _futureTime,
                NotBefore = DateTimeOffset.UtcNow.AddYears(-2)
            });
    }

    [Fact]
    public async Task GetSigningClientAsync_KeyNotActive_NoKey()
    {
        // Arrange
        var target = new SigningKeyRing(
            SystemClock.Instance,
            _keyClientFixture.KeyClient,
            _keyClientFixture.KeyName);

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => target.GetSigningClientAsync());
    }
}
