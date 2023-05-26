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
using Energinet.DataHub.MarketParticipant.Domain.Model.Users.Authentication;

namespace Energinet.DataHub.MarketParticipant.Domain.Model.Users;

public sealed class UserIdentity
{
    private readonly List<LoginIdentity> _loginIdentities;

    public UserIdentity(
        ExternalUserId id,
        EmailAddress email,
        UserStatus status,
        string firstName,
        string lastName,
        PhoneNumber? phoneNumber,
        DateTimeOffset createdDate,
        AuthenticationMethod authentication,
        IEnumerable<LoginIdentity> loginIdentities)
    {
        Id = id;
        Email = email;
        Status = status;
        FirstName = firstName;
        LastName = lastName;
        PhoneNumber = phoneNumber;
        CreatedDate = createdDate;
        Authentication = authentication;
        _loginIdentities = loginIdentities.ToList();
    }

    public UserIdentity(
        EmailAddress email,
        string firstName,
        string lastName,
        PhoneNumber phoneNumber,
        AuthenticationMethod authentication)
    {
        if (authentication == AuthenticationMethod.Undetermined)
            throw new NotSupportedException("Cannot create a user without an authentication method.");

        Id = new ExternalUserId(Guid.Empty);
        Email = email;
        Status = UserStatus.Active;
        FirstName = firstName;
        LastName = lastName;
        PhoneNumber = phoneNumber;
        CreatedDate = DateTimeOffset.UtcNow;
        Authentication = authentication;
        _loginIdentities = new List<LoginIdentity>();

        ValidateName();
    }

    public ExternalUserId Id { get; }
    public EmailAddress Email { get; }
    public UserStatus Status { get; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string FullName => $"{FirstName} {LastName}";
    public PhoneNumber? PhoneNumber { get; set; }
    public DateTimeOffset CreatedDate { get; }
    public AuthenticationMethod Authentication { get; }
    public IReadOnlyCollection<LoginIdentity> LoginIdentities => _loginIdentities;

    public void LinkOpenIdFrom(UserIdentity userIdentity)
    {
        ArgumentNullException.ThrowIfNull(userIdentity);

        if (userIdentity.Email != Email)
        {
            throw new ValidationException($"Email address of user {userIdentity.Id} does not match email address of user {Id}.");
        }

        var loginIdentityToMove = userIdentity.LoginIdentities.First(e => e.SignInType == "federated");

        _loginIdentities.Add(loginIdentityToMove);
    }

    private void ValidateName()
    {
        if (string.IsNullOrWhiteSpace(FirstName))
            throw new ValidationException("First name must not be empty.");

        if (FirstName.Length > 64)
            throw new ValidationException("First name can be at most 64 characters long.");

        if (string.IsNullOrWhiteSpace(LastName))
            throw new ValidationException("Last name must not be empty.");

        if (LastName.Length > 64)
            throw new ValidationException("Last name can be at most 64 characters long.");
    }
}
