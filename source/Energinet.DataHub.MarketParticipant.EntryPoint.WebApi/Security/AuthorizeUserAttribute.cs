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
using System.Linq;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.Infrastructure.Model.Permissions;
using Microsoft.AspNetCore.Authorization;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Security;

// TODO: Move tests.
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class AuthorizeUserAttribute : Attribute, IAuthorizeData
{
    private const string ConfigureUsingCtor = "Use the ctor to select a permission.";

    public AuthorizeUserAttribute(params PermissionId[] permissions)
    {
        Permissions = permissions;
    }

    public IEnumerable<PermissionId> Permissions { get; }

    string? IAuthorizeData.Policy
    {
        get => null;
        set => throw new InvalidOperationException(ConfigureUsingCtor);
    }

    string? IAuthorizeData.Roles
    {
        get => string.Join(",", Permissions.Select(p => KnownPermissions.All.Single(kp => kp.Id == p).Claim));
        set => throw new InvalidOperationException(ConfigureUsingCtor);
    }

    string? IAuthorizeData.AuthenticationSchemes
    {
        get => null;
        set => throw new InvalidOperationException(ConfigureUsingCtor);
    }
}
