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

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users.Authentication;

namespace Energinet.DataHub.MarketParticipant.Domain.Model.Users;

// TODO: UTs
public sealed class UserInvitation
{
    public UserInvitation(
        EmailAddress email,
        string firstName,
        string lastName,
        PhoneNumber phoneNumber,
        AuthenticationMethod requiredAuthentication,
        Actor assignedActor,
        IReadOnlyCollection<UserRole> assignedRoles)
    {
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        PhoneNumber = phoneNumber;
        RequiredAuthentication = requiredAuthentication;
        AssignedActor = assignedActor;
        AssignedRoles = assignedRoles;

        ValidateName();
        ValidateActor();
        ValidateUserRoles();
    }

    public EmailAddress Email { get; }
    public string FirstName { get; }
    public string LastName { get; }
    public PhoneNumber PhoneNumber { get; }
    public AuthenticationMethod RequiredAuthentication { get; }
    public Actor AssignedActor { get; }
    public IReadOnlyCollection<UserRole> AssignedRoles { get; }

    private void ValidateName()
    {
        if (string.IsNullOrWhiteSpace(FirstName))
            throw new ValidationException("First name must not be empty.");

        if (string.IsNullOrWhiteSpace(LastName))
            throw new ValidationException("Last name must not be empty.");
    }

    private void ValidateActor()
    {
        if (AssignedActor.Status is not ActorStatus.Active)
            throw new ValidationException($"The actor {AssignedActor.Id} has an incorrect state.");
    }

    private void ValidateUserRoles()
    {
        foreach (var userRole in AssignedRoles)
        {
            if (userRole.Status is not UserRoleStatus.Active)
                throw new ValidationException($"The user role {userRole.Id} has an incorrect state.");

            if (AssignedActor.MarketRoles.All(mr => mr.Function != userRole.EicFunction))
                throw new ValidationException($"The user role {userRole.Id} cannot be used with the actor {AssignedActor.Id}.");
        }
    }
}
