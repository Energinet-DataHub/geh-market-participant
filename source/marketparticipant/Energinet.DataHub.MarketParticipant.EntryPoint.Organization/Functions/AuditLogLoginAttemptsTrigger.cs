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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands;
using MediatR;
using Microsoft.Azure.Functions.Worker;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.Organization.Functions;

public sealed class AuditLogLoginAttemptsTrigger
{
    private readonly IMediator _mediator;

    public AuditLogLoginAttemptsTrigger(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Function(nameof(AuditLogLoginAttemptsTrigger))]
    public Task RunAsync([TimerTrigger("0 */12 * * *")] FunctionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return _mediator.Send(new AuditLogLoginAttemptLogEntriesCommand());
    }
}