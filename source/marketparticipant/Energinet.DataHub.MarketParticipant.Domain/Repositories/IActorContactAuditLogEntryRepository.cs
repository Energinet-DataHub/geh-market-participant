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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;

namespace Energinet.DataHub.MarketParticipant.Domain.Repositories;

/// <summary>
/// Repository for querying actor contact audit logs
/// </summary>
public interface IActorContactAuditLogEntryRepository
{
    /// <summary>
    /// Retrieves all log entries for a given actor.
    /// </summary>
    /// <param name="actorId">The actorId to get the contact logs for.</param>
    Task<IEnumerable<ActorContactAuditLogEntry>> GetAsync(ActorId actorId);
}