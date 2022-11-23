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

namespace Energinet.DataHub.MarketParticipant.Domain.Model.Users;

public class User
{
    public User()
    {
        Id = new UserId(Guid.Empty);
        ExternalId = new ExternalUserId(Guid.Empty);
        Name = new UserName(string.Empty);
        RoleAssignments = new List<UserRoleAssignment>();
    }

    public User(UserId id, ExternalUserId externalId, UserName name, IEnumerable<UserRoleAssignment> roleAssignments)
    {
        Id = id;
        ExternalId = externalId;
        Name = name;
        RoleAssignments = roleAssignments;
    }

    public UserId Id { get; }
    public ExternalUserId ExternalId { get; set; }
    public UserName Name { get; }
    public IEnumerable<UserRoleAssignment> RoleAssignments { get; }
}
