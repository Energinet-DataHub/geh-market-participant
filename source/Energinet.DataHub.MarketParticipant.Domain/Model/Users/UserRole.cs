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
using Energinet.DataHub.Core.App.Common.Security;

namespace Energinet.DataHub.MarketParticipant.Domain.Model.Users;

public sealed class UserRole
{
    public UserRole(
        UserRoleId id,
        string name,
        IEnumerable<EicFunction> allowedMarkedRoles,
        IEnumerable<Permission> permissions,
        string description,
        EicFunction eicFunction,
        int status)
    {
        Id = id;
        Name = name;
        AllowedMarkedRoles = allowedMarkedRoles;
        Permissions = permissions;
        Description = description;
        EicFunction = eicFunction;
        Status = status;
    }

    public UserRoleId Id { get; }
    public string Name { get; set; }
    public IEnumerable<EicFunction> AllowedMarkedRoles { get; }
    public IEnumerable<Permission> Permissions { get; }
    public string Description { get; set; }
    public EicFunction EicFunction { get; }
    public int Status { get; set; }
}
