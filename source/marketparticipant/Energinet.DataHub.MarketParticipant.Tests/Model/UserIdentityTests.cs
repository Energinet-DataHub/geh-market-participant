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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users.Authentication;
using Energinet.DataHub.MarketParticipant.Tests.Common;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Model;

[UnitTest]
public sealed class UserIdentityTests
{
    private const string ValidFirstName = "John";
    private const string ValidLastName = "Doe";
    private readonly PhoneNumber _validPhoneNumber = new("+45 00000000");
    private readonly EmailAddress _validEmailAddress = new("todo@todo.dk");
    private readonly AuthenticationMethod _validAuthentication = new SmsAuthenticationMethod(new PhoneNumber("+45 71000000"));
    private readonly IEnumerable<LoginIdentity> _validLoginIdentities = new List<LoginIdentity> { new("emailAddress", "issuer", "issuerAssignedId") };

    [Fact]
    public void Ctor_UserIdentityTests_ValidatesAuthenticationMethod()
    {
        Assert.Throws<NotSupportedException>(() => new UserIdentity(
            _validEmailAddress,
            ValidFirstName,
            ValidLastName,
            _validPhoneNumber,
            AuthenticationMethod.Undetermined));
    }

    [Theory]
    [InlineData("", false)]
    [InlineData(" ", false)]
    [InlineData(null, false)]
    [InlineData("John", true)]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", true)]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", false)]
    public void Ctor_UserIdentityTests_ValidatesFirstName(string value, bool isValid)
    {
        if (isValid)
        {
            Assert.Equal(value, new UserIdentity(_validEmailAddress, value, ValidLastName, _validPhoneNumber, _validAuthentication).FirstName);
        }
        else
        {
            Assert.Throws<ValidationException>(() => new UserIdentity(
                _validEmailAddress,
                value,
                ValidLastName,
                _validPhoneNumber,
                _validAuthentication));
        }
    }

    [Theory]
    [InlineData("", false)]
    [InlineData(" ", false)]
    [InlineData(null, false)]
    [InlineData("Doe", true)]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", true)]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", false)]
    public void Ctor_UserIdentityTests_ValidatesLastName(string value, bool isValid)
    {
        if (isValid)
        {
            Assert.Equal(value, new UserIdentity(_validEmailAddress, ValidFirstName, value, _validPhoneNumber, _validAuthentication).LastName);
        }
        else
        {
            Assert.Throws<ValidationException>(() => new UserIdentity(
                _validEmailAddress,
                ValidFirstName,
                value,
                _validPhoneNumber,
                _validAuthentication));
        }
    }

    [Fact]
    public void UserIdentityTests_LinkOpenIdFrom_SameEmail()
    {
        // Arrange
        var validUserIdentity = new UserIdentity(
            new ExternalUserId(Guid.NewGuid()),
            _validEmailAddress,
            UserStatus.Active,
            ValidFirstName,
            ValidLastName,
            _validPhoneNumber,
            DateTimeOffset.UtcNow,
            _validAuthentication,
            _validLoginIdentities);

        var validUserIdentityToLink = new UserIdentity(
            new ExternalUserId(Guid.NewGuid()),
            _validEmailAddress,
            UserStatus.Active,
            ValidFirstName,
            ValidLastName,
            _validPhoneNumber,
            DateTimeOffset.UtcNow,
            _validAuthentication,
            new List<LoginIdentity> { new("federated", "issuer", "issuerAssignedId") });

        // Act
        validUserIdentity.LinkOpenIdFrom(validUserIdentityToLink);

        // Assert
        Assert.Equal(2, validUserIdentity.LoginIdentities.Count);
        Assert.Single(validUserIdentity.LoginIdentities, e => e.SignInType == "emailAddress");
        Assert.Single(validUserIdentity.LoginIdentities, e => e.SignInType == "federated");
    }

    [Fact]
    public void UserIdentityTests_LinkOpenIdFrom_NotSameEmail()
    {
        // Arrange
        var validUserIdentity = new UserIdentity(
            new ExternalUserId(Guid.NewGuid()),
            _validEmailAddress,
            UserStatus.Active,
            ValidFirstName,
            ValidLastName,
            _validPhoneNumber,
            DateTimeOffset.UtcNow,
            _validAuthentication,
            _validLoginIdentities);

        var validUserIdentityToLink = new UserIdentity(
            new ExternalUserId(Guid.NewGuid()),
            new EmailAddress("notsameemail@notsame.dk"),
            UserStatus.Active,
            ValidFirstName,
            ValidLastName,
            _validPhoneNumber,
            DateTimeOffset.UtcNow,
            _validAuthentication,
            new List<LoginIdentity> { new("federated", "issuer", "issuerAssignedId") });

        // Act + Assert
        Assert.Throws<ValidationException>(() => validUserIdentity.LinkOpenIdFrom(validUserIdentityToLink));
    }
}
