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
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application;
using Energinet.DataHub.MarketParticipant.Application.Commands;
using Energinet.DataHub.MarketParticipant.Application.Handlers;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.RevisionLog.Integration;
using Moq;
using NodaTime;
using NodaTime.Extensions;
using NodaTime.Serialization.SystemTextJson;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Handlers;

[UnitTest]
public sealed class AuditLoginAttemptsHandlerTests
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions()
        .ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);

    [Fact]
    public async Task Handle_LoginAttemptsFound_LogsToRevisionLogApi()
    {
        // arrange
        var expected = CreateB2CLogEntry();

        var b2CLogRepository = new Mock<IB2CLogRepository>();
        b2CLogRepository
            .Setup(x => x.GetLoginAttempsAsync(It.IsAny<Instant>()))
            .Returns(ToAsyncEnumerable([expected]));

        var cutoffRepository = new Mock<ICutoffRepository>();
        var revisionLogClient = new Mock<IRevisionLogClient>();

        var target = new AuditLoginAttemptsHandler(b2CLogRepository.Object, cutoffRepository.Object, revisionLogClient.Object);

        // act
        await target.Handle(new AuditLoginAttemptsCommand(), CancellationToken.None);

        // assert
        revisionLogClient.Verify(
            x => x.LogAsync(It.Is<RevisionLogEntry>(y =>
                y.LogId == Guid.Parse(expected.Id) &&
                y.OccurredOn == expected.AttemptedAt &&
                y.SystemId == SubsystemInformation.Id &&
                y.Activity == "LoginAttempt" &&
                y.Origin == "B2C Login Audit Log" &&
                y.Payload == JsonSerializer.Serialize(expected, _jsonSerializerOptions))),
            Times.Once);
    }

    [Fact]
    public async Task Handle_LoginAttemptsFound_UpdatesCutoffToNewestLogTimestamp()
    {
        // arrange
        var expected = Instant.FromUtc(2024, 1, 1, 10, 59);

        var b2CLogRepository = new Mock<IB2CLogRepository>();
        b2CLogRepository
            .Setup(x => x.GetLoginAttempsAsync(It.IsAny<Instant>()))
            .Returns(
                ToAsyncEnumerable(
                [
                    CreateB2CLogEntry(expected.Minus(Duration.FromSeconds(1))),
                    CreateB2CLogEntry(expected),
                    CreateB2CLogEntry(expected.Minus(Duration.FromSeconds(2)))
                ]));

        var cutoffRepository = new Mock<ICutoffRepository>();
        var revisionLogClient = new Mock<IRevisionLogClient>();

        var target = new AuditLoginAttemptsHandler(b2CLogRepository.Object, cutoffRepository.Object, revisionLogClient.Object);

        // act
        await target.Handle(new AuditLoginAttemptsCommand(), CancellationToken.None);

        // assert
        cutoffRepository.Verify(x => x.UpdateCutoffAsync(CutoffType.B2CLoginAttempt, expected), Times.Once);
    }

    private static B2CLoginAttemptLogEntry CreateB2CLogEntry(Instant? attemptedAt = null)
    {
        return new B2CLoginAttemptLogEntry(
            Guid.NewGuid().ToString(),
            attemptedAt ?? DateTimeOffset.Now.ToInstant(),
            "127.0.0.1",
            "DK",
            Guid.NewGuid().ToString(),
            "jd@629FF37F-B4B9-4111-B2E2-B81D3EE1CD6A.com",
            Guid.NewGuid().ToString(),
            "resource",
            10,
            "NOT GOOD!");
    }

    private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(IEnumerable<T> enumerable)
    {
        await Task.CompletedTask;

        foreach (var element in enumerable)
        {
            yield return element;
        }
    }
}
