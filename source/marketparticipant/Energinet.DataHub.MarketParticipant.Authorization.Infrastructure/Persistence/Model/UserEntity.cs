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
using System.Collections.ObjectModel;

namespace Energinet.DataHub.MarketParticipant.Authorization.Infrastructure.Persistence.Model;

public sealed class UserEntity
{
    public Guid Id { get; set; }
    public Guid ExternalId { get; set; }
    public Guid AdministratedByActorId { get; set; }
    public Guid SharedReferenceId { get; set; }
    public string Email { get; set; } = null!;
    public Collection<UserRoleAssignmentEntity> RoleAssignments { get; init; } = new();
    public DateTimeOffset? MitIdSignupInitiatedAt { get; set; }
    public DateTimeOffset? InvitationExpiresAt { get; set; }
    public DateTimeOffset? LatestLoginAt { get; set; }
}
