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
using Energinet.DataHub.MarketParticipant.Domain.Model.Users.Authentication;

namespace Energinet.DataHub.MarketParticipant.Domain.Model.Users;

public sealed class UserIdentity
{
    public UserIdentity(
        ExternalUserId id,
        EmailAddress email,
        UserStatus status,
        string firstName,
        string lastName,
        PhoneNumber? phoneNumber,
        DateTimeOffset createdDate,
        AuthenticationMethod authentication)
    {
        Id = id;
        Email = email;
        Status = status;
        FirstName = firstName;
        LastName = lastName;
        PhoneNumber = phoneNumber;
        CreatedDate = createdDate;
        Authentication = authentication;
    }

    public UserIdentity(
        EmailAddress email,
        string firstName,
        string lastName,
        PhoneNumber phoneNumber,
        AuthenticationMethod authentication)
    {
        Id = new ExternalUserId(Guid.Empty);
        Email = email;
        Status = UserStatus.Active;
        FirstName = firstName;
        LastName = lastName;
        PhoneNumber = phoneNumber;
        CreatedDate = DateTimeOffset.UtcNow;
        Authentication = authentication;
    }

    public ExternalUserId Id { get; }
    public EmailAddress Email { get; }
    public UserStatus Status { get; }
    public string FirstName { get; }
    public string LastName { get; }
    public string FullName => $"{FirstName} {LastName}";
    public PhoneNumber? PhoneNumber { get; }
    public DateTimeOffset CreatedDate { get; }
    public AuthenticationMethod Authentication { get; }
}
