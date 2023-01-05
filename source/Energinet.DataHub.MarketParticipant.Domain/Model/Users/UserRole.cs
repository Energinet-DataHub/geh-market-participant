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
using Energinet.DataHub.Core.App.Common.Security;

namespace Energinet.DataHub.MarketParticipant.Domain.Model.Users;

public sealed class UserRole
{
    public UserRole(
        string name,
        string description,
        UserRoleStatus status,
        IEnumerable<Permission> permissions,
        EicFunction eicFunction)
    {
        Id = new UserRoleId(Guid.Empty);
        Name = name;
        Permissions = permissions;
        Description = description;
        Status = status;
        EicFunction = eicFunction;
    }

    public UserRole(
        UserRoleId id,
        string name,
        string description,
        UserRoleStatus status,
        IEnumerable<Permission> permissions,
        EicFunction eicFunction)
    {
        Id = id;
        Name = name;
        Permissions = permissions;
        Description = description;
        Status = status;
        EicFunction = eicFunction;
    }

    public UserRoleId Id { get; }
    public string Name { get; set; }
    public string Description { get; set; }
    public UserRoleStatus Status { get; set; }
    public IEnumerable<Permission> Permissions { get; }
    public EicFunction EicFunction { get; }
}
