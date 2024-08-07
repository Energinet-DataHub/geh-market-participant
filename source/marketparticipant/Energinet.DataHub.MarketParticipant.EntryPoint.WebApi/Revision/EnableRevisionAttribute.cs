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
using Microsoft.AspNetCore.Http;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Revision;

[AttributeUsage(AttributeTargets.Method)]
public sealed class EnableRevisionAttribute : Attribute
{
    public EnableRevisionAttribute(string activityName, Type entityType)
    {
        ArgumentNullException.ThrowIfNull(entityType);

        ActivityName = activityName;
        EntityType = entityType;
    }

    public EnableRevisionAttribute(string activityName, Type entityType, string keyRouteParam)
    {
        ArgumentNullException.ThrowIfNull(entityType);

        ActivityName = activityName;
        EntityType = entityType;
        KeyRouteParam = keyRouteParam;
    }

    public string ActivityName { get; }

    public Type EntityType { get; }

    public string? KeyRouteParam { get; }

    internal string LookupEntityKey(HttpRequest httpRequest)
    {
        if (KeyRouteParam == null)
            return "[no entity key]";

        return httpRequest.RouteValues[KeyRouteParam]?.ToString() ?? "[null]";
    }
}
