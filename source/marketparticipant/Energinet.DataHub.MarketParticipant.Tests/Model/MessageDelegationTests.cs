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
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Delegations;
using Energinet.DataHub.MarketParticipant.Tests.Common;
using NodaTime;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Model;

[UnitTest]
public sealed class MessageDelegationTests
{
    // Test data for the overlap test
    // ReSharper disable once InconsistentNaming
#pragma warning disable SA1401
#pragma warning disable CA2211
    public static TheoryData<IReadOnlyList<(Instant StartsAt, Instant? StopsAt)>, bool> OverlapCases =
#pragma warning restore CA2211
#pragma warning restore SA1401
        new()
        {
            // Two delegations after each other, no overlap, new starts after old stops, should not throw
            {
                new List<(Instant StartsAt, Instant? StopsAt)>
                {
                    (Instant.FromDateTimeOffset(DateTimeOffset.UtcNow), Instant.FromDateTimeOffset(DateTimeOffset.UtcNow.AddMonths(1))),
                    (Instant.FromDateTimeOffset(DateTimeOffset.UtcNow.AddMonths(2)), null)
                },
                false
            },

            // Two delegations after each other, new start right as old stops, should not throw
            {
                new List<(Instant StartsAt, Instant? StopsAt)>
                {
                    (Instant.FromDateTimeOffset(DateTimeOffset.UtcNow), Instant.FromDateTimeOffset(DateTimeOffset.UtcNow.AddMonths(1))),
                    (Instant.FromDateTimeOffset(DateTimeOffset.UtcNow.AddMonths(1)), Instant.FromDateTimeOffset(DateTimeOffset.UtcNow.AddMonths(2)))
                },
                false
            },

            // Two delegations, one is cancelled, no overlap, should not throw
            {
                new List<(Instant StartsAt, Instant? StopsAt)>
                {
                    (Instant.FromDateTimeOffset(DateTimeOffset.UtcNow), Instant.FromDateTimeOffset(DateTimeOffset.UtcNow.AddMonths(-2))),
                    (Instant.FromDateTimeOffset(DateTimeOffset.UtcNow), Instant.FromDateTimeOffset(DateTimeOffset.UtcNow.AddMonths(2)))
                },
                false
            },
        };

    [Fact]
    public void Validate_NoPeriodOverlap_CreatesValidDomainObject()
    {
        // arrange
        var testData = CreateTestBasicTestData();
        var messageDelegation = new MessageDelegation(testData.ActorFrom, DelegationMessageType.Rsm012Inbound);

        // act
        messageDelegation.DelegateTo(testData.ActorTo.Id, testData.GridArea.Id, Instant.FromDateTimeOffset(DateTimeOffset.Now), Instant.FromDateTimeOffset(DateTimeOffset.Now.AddMonths(1)));
        messageDelegation.DelegateTo(testData.ActorTo.Id, testData.GridArea.Id, Instant.FromDateTimeOffset(DateTimeOffset.Now.AddMonths(2)), null);

        // assert
        Assert.Equal(2, messageDelegation.Delegations.Count);
    }

    [Theory]
    [MemberData(nameof(OverlapCases))]
    public void Validate_PeriodOverlap_HandlesCorrectly(IReadOnlyList<(Instant StartsAt, Instant? StopsAt)> delegationPeriods, bool shouldThrow)
    {
        // arrange
        var testData = CreateTestBasicTestData();
        var messageDelegation = new MessageDelegation(testData.ActorFrom, DelegationMessageType.Rsm012Inbound);
        var exceptions = new List<Exception>();

        // act
        foreach (var delegationPeriod in delegationPeriods)
        {
            var exception = Record.Exception(() =>
                messageDelegation.DelegateTo(
                    testData.ActorTo.Id,
                    testData.GridArea.Id,
                    delegationPeriod.StartsAt,
                    delegationPeriod.StopsAt));

            if (exception != null)
                exceptions.Add(exception);
        }

        // assert
        if (shouldThrow)
        {
            Assert.NotEmpty(exceptions);
            Assert.Contains(typeof(ValidationException), exceptions.Select(e => e?.GetType()));
        }
        else
        {
            Assert.Empty(exceptions);
            Assert.Equal(delegationPeriods.Count, messageDelegation.Delegations.Count);
        }
    }

    private static (Actor ActorFrom, Actor ActorTo, GridArea GridArea) CreateTestBasicTestData()
    {
        var actorFrom = new Actor(
            new ActorId(Guid.NewGuid()),
            new OrganizationId(Guid.NewGuid()),
            null,
            new MockedGln(),
            ActorStatus.Active,
            new List<ActorMarketRole> { new(EicFunction.EnergySupplier, Enumerable.Empty<ActorGridArea>()) },
            new ActorName("fake_value"),
            null);
        var actorTo = new Actor(
            new ActorId(Guid.NewGuid()),
            new OrganizationId(Guid.NewGuid()),
            null,
            new MockedGln(),
            ActorStatus.Active,
            new List<ActorMarketRole> { new(EicFunction.EnergySupplier, Enumerable.Empty<ActorGridArea>()) },
            new ActorName("fake_value"),
            null);
        var gridArea = new GridArea(
            new GridAreaId(Guid.NewGuid()),
            new GridAreaName("Mock"),
            new GridAreaCode("001"),
            PriceAreaCode.Dk2,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddYears(1));
        return (actorFrom, actorTo, gridArea);
    }
}
