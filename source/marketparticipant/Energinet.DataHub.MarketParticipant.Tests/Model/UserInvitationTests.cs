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
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users.Authentication;
using Energinet.DataHub.MarketParticipant.Tests.Common;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Model;

[UnitTest]
public sealed class UserInvitationTests
{
    private readonly UserInvitation _validInvitation = new(
        new MockedEmailAddress(),
        "John",
        "Doe",
        new PhoneNumber("00000000"),
        new SmsAuthenticationMethod(new PhoneNumber("+45 00000000")),
        new Actor(
            new ActorId(Guid.NewGuid()),
            new OrganizationId(Guid.NewGuid()),
            null,
            new MockedGln(),
            ActorStatus.New,
            new[] { new ActorMarketRole(EicFunction.BalanceResponsibleParty) },
            new ActorName("fake_value"),
            new Collection<ActorCredentials>()),
        new[]
        {
            new UserRole(
                "fake_value",
                "fake_value",
                UserRoleStatus.Active,
                Array.Empty<PermissionId>(),
                EicFunction.BalanceResponsibleParty)
        });

    [Fact]
    public void Create_ValidInvitation_ReturnsInvitation()
    {
        // Arrange + Act
        var actual = new UserInvitation(
            _validInvitation.Email,
            _validInvitation.FirstName,
            _validInvitation.LastName,
            _validInvitation.PhoneNumber,
            _validInvitation.RequiredAuthentication,
            _validInvitation.AssignedActor,
            _validInvitation.AssignedRoles);

        // Assert
        Assert.Equivalent(_validInvitation, actual);
    }

    [Theory]
    [InlineData(ActorStatus.New, true)]
    [InlineData(ActorStatus.Passive, true)]
    [InlineData(ActorStatus.Active, true)]
    [InlineData(ActorStatus.Inactive, false)]
    public void Create_ActorStatus_IsVerified(ActorStatus status, bool isValid)
    {
        // Arrange
        var testActor = new Actor(
            new ActorId(Guid.NewGuid()),
            new OrganizationId(Guid.NewGuid()),
            null,
            new MockedGln(),
            status,
            new[] { new ActorMarketRole(EicFunction.BalanceResponsibleParty) },
            new ActorName("fake_value"),
            new Collection<ActorCredentials>());

        // Act + Assert
        if (isValid)
        {
            var actual = new UserInvitation(
                _validInvitation.Email,
                _validInvitation.FirstName,
                _validInvitation.LastName,
                _validInvitation.PhoneNumber,
                _validInvitation.RequiredAuthentication,
                testActor,
                _validInvitation.AssignedRoles);

            Assert.NotNull(actual);
        }
        else
        {
            Assert.Throws<ValidationException>(() => new UserInvitation(
                _validInvitation.Email,
                _validInvitation.FirstName,
                _validInvitation.LastName,
                _validInvitation.PhoneNumber,
                _validInvitation.RequiredAuthentication,
                testActor,
                _validInvitation.AssignedRoles));
        }
    }

    [Theory]
    [InlineData(UserRoleStatus.Active, true)]
    [InlineData(UserRoleStatus.Inactive, false)]
    public void Create_UserRoleInactive_IsVerified(UserRoleStatus status, bool isValid)
    {
        // Arrange
        var testRole = new UserRole(
            "fake_value",
            "fake_value",
            status,
            Array.Empty<PermissionId>(),
            EicFunction.BalanceResponsibleParty);

        // Act + Assert
        if (isValid)
        {
            var actual = new UserInvitation(
                _validInvitation.Email,
                _validInvitation.FirstName,
                _validInvitation.LastName,
                _validInvitation.PhoneNumber,
                _validInvitation.RequiredAuthentication,
                _validInvitation.AssignedActor,
                new[] { testRole });

            Assert.NotNull(actual);
        }
        else
        {
            Assert.Throws<ValidationException>(() => new UserInvitation(
                _validInvitation.Email,
                _validInvitation.FirstName,
                _validInvitation.LastName,
                _validInvitation.PhoneNumber,
                _validInvitation.RequiredAuthentication,
                _validInvitation.AssignedActor,
                new[] { testRole }));
        }
    }

    [Theory]
    [InlineData(EicFunction.BillingAgent, false)]
    [InlineData(EicFunction.MeteredDataResponsible, false)]
    [InlineData(EicFunction.BalanceResponsibleParty, true)]
    public void Create_UserRoleEicFunction_IsVerified(EicFunction eicFunction, bool isValid)
    {
        // Arrange
        var testRole = new UserRole(
            "fake_value",
            "fake_value",
            UserRoleStatus.Active,
            Array.Empty<PermissionId>(),
            eicFunction);

        // Act + Assert
        if (isValid)
        {
            var actual = new UserInvitation(
                _validInvitation.Email,
                _validInvitation.FirstName,
                _validInvitation.LastName,
                _validInvitation.PhoneNumber,
                _validInvitation.RequiredAuthentication,
                _validInvitation.AssignedActor,
                new[] { testRole });

            Assert.NotNull(actual);
        }
        else
        {
            Assert.Throws<ValidationException>(() => new UserInvitation(
                _validInvitation.Email,
                _validInvitation.FirstName,
                _validInvitation.LastName,
                _validInvitation.PhoneNumber,
                _validInvitation.RequiredAuthentication,
                _validInvitation.AssignedActor,
                new[] { testRole }));
        }
    }
}
