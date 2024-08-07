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

using System.Collections.Generic;
using NodaTime;

#pragma warning disable CA1711 // Rename type name Permission so that it does not end in 'Permission'.

namespace Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;

public sealed class Permission
{
    public Permission(
        PermissionId id,
        string claim,
        string description,
        Instant created,
        IReadOnlyCollection<EicFunction> assignableTo)
    {
        Id = id;
        Claim = claim;
        Description = description;
        Created = created;
        AssignableTo = assignableTo;
    }

    public PermissionId Id { get; }
    public string Name => Claim;
    public string Claim { get; }
    public string Description { get; set; }
    public Instant Created { get; }
    public IReadOnlyCollection<EicFunction> AssignableTo { get; }

    public override string ToString()
    {
        return Name;
    }
}

#pragma warning restore CA1711
