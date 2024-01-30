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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.User;
using Energinet.DataHub.MarketParticipant.Application.Validation;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Validation;

[UnitTest]
public sealed class InviteUserCommandRuleSetTests
{
    private readonly UserInvitationDto _validInvitation = new(
        "fake@value",
        new UserDetailsDto(
            "fake_value",
            "fake_value",
            "+45 00000000"),
        Guid.NewGuid(),
        new[] { Guid.NewGuid() });

    private readonly Guid _validInvitedByUserId = Guid.NewGuid();

    [Fact]
    public async Task Validate_Invitation_ValidatesProperty()
    {
        // Arrange
        const string propertyName = nameof(InviteUserCommand.Invitation);

        var target = new InviteUserCommandRuleSet();
        var command = new InviteUserCommand(null!, Guid.Empty);

        // Act
        var result = await target.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
    }

    [Theory]
    [InlineData("", false)]
    [InlineData(" ", false)]
    [InlineData(null, false)]
    [InlineData("fake_value", false)]
    [InlineData("fake_value.com", false)]
    [InlineData("fake@value", true)]
    [InlineData("fake_max0000000000000000000000000000000000000000000000000_@value", true)]
    [InlineData("fake_00000000000000000000000000000000000000000000000000000_@value", false)]
    public async Task Validate_InvitationEmail_ValidatesProperty(string? value, bool isValid)
    {
        // Arrange
        const string propertyName = $"{nameof(InviteUserCommand.Invitation)}.{nameof(UserInvitationDto.Email)}";

        var target = new InviteUserCommandRuleSet();
        var invitation = _validInvitation with { Email = value! };

        var command = new InviteUserCommand(invitation, _validInvitedByUserId);

        // Act
        var result = await target.ValidateAsync(command);

        // Assert
        if (isValid)
        {
            Assert.True(result.IsValid);
            Assert.DoesNotContain(propertyName, result.Errors.Select(x => x.PropertyName));
        }
        else
        {
            Assert.False(result.IsValid);
            Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
        }
    }

    [Theory]
    [InlineData("", false)]
    [InlineData(" ", false)]
    [InlineData(null, false)]
    [InlineData("John", true)]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", true)]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", false)]
    public async Task Validate_InvitationFirstName_ValidatesProperty(string? value, bool isValid)
    {
        // Arrange
        const string propertyName = $"{nameof(InviteUserCommand.Invitation)}.{nameof(UserInvitationDto.UserDetails)}.{nameof(UserInvitationDto.UserDetails.FirstName)}";

        var target = new InviteUserCommandRuleSet();
        var invitation = _validInvitation with
        {
            UserDetails = _validInvitation.UserDetails! with
            {
                FirstName = value!,
            },
        };

        var command = new InviteUserCommand(invitation, _validInvitedByUserId);

        // Act
        var result = await target.ValidateAsync(command);

        // Assert
        if (isValid)
        {
            Assert.True(result.IsValid);
            Assert.DoesNotContain(propertyName, result.Errors.Select(x => x.PropertyName));
        }
        else
        {
            Assert.False(result.IsValid);
            Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
        }
    }

    [Theory]
    [InlineData("", false)]
    [InlineData(" ", false)]
    [InlineData(null, false)]
    [InlineData("Doe", true)]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", true)]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", false)]
    public async Task Validate_InvitationLastName_ValidatesProperty(string? value, bool isValid)
    {
        // Arrange
        const string propertyName = $"{nameof(InviteUserCommand.Invitation)}.{nameof(UserInvitationDto.UserDetails)}.{nameof(UserInvitationDto.UserDetails.LastName)}";

        var target = new InviteUserCommandRuleSet();
        var invitation = _validInvitation with
        {
            UserDetails = _validInvitation.UserDetails! with
            {
                LastName = value!,
            },
        };

        var command = new InviteUserCommand(invitation, _validInvitedByUserId);

        // Act
        var result = await target.ValidateAsync(command);

        // Assert
        if (isValid)
        {
            Assert.True(result.IsValid);
            Assert.DoesNotContain(propertyName, result.Errors.Select(x => x.PropertyName));
        }
        else
        {
            Assert.False(result.IsValid);
            Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
        }
    }

    [Theory]
    [InlineData("", false)]
    [InlineData(" ", false)]
    [InlineData(null, false)]
    [InlineData("+4500000000", false)]
    [InlineData("+00000000", false)]
    [InlineData("00000000", false)]
    [InlineData("+ 00000000", false)]
    [InlineData("+45 ", false)]
    [InlineData(" +45 00000000", false)]
    [InlineData("+45 00000000 ", false)]
    [InlineData("+45 00000000", true)]
    [InlineData("+123 00000000", true)]
    [InlineData("+45 00000000000000000000000000", true)]
    [InlineData("+45 000000000000000000000000000", false)]
    public async Task Validate_InvitationPhoneNumber_ValidatesProperty(string? value, bool isValid)
    {
        // Arrange
        const string propertyName = $"{nameof(InviteUserCommand.Invitation)}.{nameof(UserInvitationDto.UserDetails)}.{nameof(UserInvitationDto.UserDetails.PhoneNumber)}";

        var target = new InviteUserCommandRuleSet();
        var invitation = _validInvitation with
        {
            UserDetails = _validInvitation.UserDetails! with
            {
                PhoneNumber = value!,
            },
        };

        var command = new InviteUserCommand(invitation, _validInvitedByUserId);

        // Act
        var result = await target.ValidateAsync(command);

        // Assert
        if (isValid)
        {
            Assert.True(result.IsValid);
            Assert.DoesNotContain(propertyName, result.Errors.Select(x => x.PropertyName));
        }
        else
        {
            Assert.False(result.IsValid);
            Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
        }
    }

    [Fact]
    public async Task Validate_InvitationAssignedActor_ValidatesProperty()
    {
        // Arrange
        const string propertyName = $"{nameof(InviteUserCommand.Invitation)}.{nameof(UserInvitationDto.AssignedActor)}";

        var target = new InviteUserCommandRuleSet();
        var invitation = _validInvitation with { AssignedActor = Guid.Empty };

        var command = new InviteUserCommand(invitation, _validInvitedByUserId);

        // Act
        var result = await target.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
    }

    [Fact]
    public async Task Validate_InvitationAssignedRoles_ValidatesProperty()
    {
        // Arrange
        const string propertyName = $"{nameof(InviteUserCommand.Invitation)}.{nameof(UserInvitationDto.AssignedRoles)}";

        var target = new InviteUserCommandRuleSet();
        var invitation = _validInvitation with { AssignedRoles = Array.Empty<Guid>() };

        var command = new InviteUserCommand(invitation, _validInvitedByUserId);

        // Act
        var result = await target.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
    }

    [Fact]
    public async Task Validate_InvitationAssignedRolesContainsEmpty_ValidatesProperty()
    {
        // Arrange
        const string propertyName = $"{nameof(InviteUserCommand.Invitation)}.{nameof(UserInvitationDto.AssignedRoles)}[1]";

        var target = new InviteUserCommandRuleSet();
        var invitation = _validInvitation with { AssignedRoles = new[] { Guid.NewGuid(), Guid.Empty } };

        var command = new InviteUserCommand(invitation, _validInvitedByUserId);

        // Act
        var result = await target.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
    }
}
