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
using System.Security.Claims;

namespace Energinet.DataHub.MarketParticipant.Application.Security;

public sealed class FrontendUser
{
    public FrontendUser(Guid userId, Guid organizationId, Guid actorId, bool isFas, IEnumerable<Claim>? claim = null)
    {
        UserId = userId;
        OrganizationId = organizationId;
        ActorId = actorId;
        IsFas = isFas;
        Claims = claim;
    }

    public Guid UserId { get; }
    public Guid OrganizationId { get; }
    public Guid ActorId { get; }
    public bool IsFas { get; }
    public IEnumerable<Claim>? Claims { get; }

    public bool IsFasOrAssignedToOrganization(Guid organizationId)
    {
        return IsFas || organizationId == OrganizationId;
    }

    public bool IsFasOrAssignedToActor(Guid actorId)
    {
        return IsFas || actorId == ActorId;
    }

    public bool IsAssignedToActor(Guid actorId)
    {
        return actorId == ActorId;
    }
}
