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

namespace Energinet.DataHub.MarketParticipant.Domain.Model.Users;

public sealed class UserRoleAssignmentAuditLogEntry
{
    public UserRoleAssignmentAuditLogEntry(
        UserId userId,
        Guid actorId,
        UserRoleTemplateId userRoleTemplateId,
        UserId changedByUserId,
        DateTimeOffset timestamp,
        UserRoleAssignmentTypeAuditLog assignmentType)
    {
        UserId = userId;
        ActorId = actorId;
        UserRoleTemplateId = userRoleTemplateId;
        Timestamp = timestamp;
        ChangedByUserId = changedByUserId;
        AssignmentType = assignmentType;
    }

    public UserId UserId { get; }
    public Guid ActorId { get; }
    public UserRoleTemplateId UserRoleTemplateId { get; }
    public UserId ChangedByUserId { get; }
    public DateTimeOffset Timestamp { get; }
    public UserRoleAssignmentTypeAuditLog AssignmentType { get; }
}
