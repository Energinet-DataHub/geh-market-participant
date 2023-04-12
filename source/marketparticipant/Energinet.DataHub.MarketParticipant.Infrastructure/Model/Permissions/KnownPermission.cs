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
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using NodaTime;

#pragma warning disable CA1711 // Rename type name Permission so that it does not end in 'Permission'.

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Model.Permissions;

/// <summary>
/// Represents a known permission. All permissions must be registered in <see cref="KnownPermissions"/> class.
/// </summary>
/// <param name="Id">The id of the permission.</param>
/// <param name="Claim">The claim to be used for the given permission. The claim will be sent with the token and shown to the users in the UI. Use a simple and concise value; the convention is [kebab-cased-plural-feature-name]:[access], e.g. grid-areas:manage or organizations:view.</param>
/// <param name="Created">The date for when the permission has been introduced. Used for audit logs only.</param>
/// <param name="AssignableTo">A list of market roles that may support the permission. If a given actor or a given user role does not belong to any of the market roles on this list, the permission will not apply to them.</param>
public sealed record KnownPermission(
    PermissionId Id,
    string Claim,
    Instant Created,
    IReadOnlyCollection<EicFunction> AssignableTo);

#pragma warning restore CA1711
