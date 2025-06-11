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
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;

namespace Energinet.DataHub.MarketParticipant.Application.Services;

/// <summary>
/// An IAuditIdentityProvider should always be registered so that all dependencies are valid and can be validated.
/// If auditing should not be used, register <see cref="ForbiddenAuditIdentityProvider"/>.
/// This will satisfy the dependencies, but throw an exception is something tries to modify audited entities.
/// </summary>
public sealed class ForbiddenAuditIdentityProvider : IAuditIdentityProvider
{
    public AuditIdentity IdentityId => throw new InvalidOperationException("A component that does not support audit tried to make changes.");
}
