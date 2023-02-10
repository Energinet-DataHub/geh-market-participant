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
using System.Linq;

namespace Energinet.DataHub.MarketParticipant.Domain.Model.Users;

// TODO: User
public sealed class User
{
    public User(
        ExternalUserId externalId,
        EmailAddress email)
    {
        Id = new UserId(Guid.Empty);
        ExternalId = externalId;
#pragma warning disable CS0618 // Type or member is obsolete
        Email = email;
#pragma warning restore CS0618 // Type or member is obsolete
        RoleAssignments = new HashSet<UserRoleAssignment>();
    }

    public User(
        UserId id,
        ExternalUserId externalId,
        EmailAddress email,
        IEnumerable<UserRoleAssignment> roleAssignments)
    {
        Id = id;
        ExternalId = externalId;
#pragma warning disable CS0618 // Type or member is obsolete
        Email = email;
#pragma warning restore CS0618 // Type or member is obsolete
        RoleAssignments = roleAssignments.ToHashSet();
    }

    public UserId Id { get; }
    public ExternalUserId ExternalId { get; set; }

    [Obsolete("TODO: Use email from UserIdentity")]
    public EmailAddress Email { get; }

    public ICollection<UserRoleAssignment> RoleAssignments { get; }
}
