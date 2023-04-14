﻿// Copyright 2020 Energinet DataHub A/S
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

public sealed class User
{
    public User(ExternalUserId externalId)
    {
        Id = new UserId(Guid.Empty);
        ExternalId = externalId;
        RoleAssignments = new HashSet<UserRoleAssignment>();
    }

    public User(
        UserId id,
        ExternalUserId externalId,
        IEnumerable<UserRoleAssignment> roleAssignments)
    {
        Id = id;
        ExternalId = externalId;
        RoleAssignments = roleAssignments.ToHashSet();
    }

    public UserId Id { get; }
    public ExternalUserId ExternalId { get; set; }
    public ICollection<UserRoleAssignment> RoleAssignments { get; }
}