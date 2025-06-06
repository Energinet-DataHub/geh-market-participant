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

using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.BalanceResponsibility;
using MediatR;
using Microsoft.Azure.Functions.Worker;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.Organization.Functions;

internal sealed class ValidateBalanceResponsibilitiesTimerTrigger
{
    private readonly IMediator _mediator;

    public ValidateBalanceResponsibilitiesTimerTrigger(IMediator mediator)
    {
        _mediator = mediator;
    }

    // NOTE: Changing the schedule changes how often notifications are sent.
    [Function(nameof(ValidateBalanceResponsibilitiesTimerTrigger))]
    public Task RunAsync([TimerTrigger("0 5 * * *")] FunctionContext context)
    {
        return _mediator.Send(new ValidateBalanceResponsibilitiesCommand());
    }
}
