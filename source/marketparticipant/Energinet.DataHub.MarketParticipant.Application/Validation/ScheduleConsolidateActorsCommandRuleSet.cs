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
using Energinet.DataHub.MarketParticipant.Application.Commands.Actors;
using FluentValidation;

namespace Energinet.DataHub.MarketParticipant.Application.Validation;

public sealed class ScheduleConsolidateActorsCommandRuleSet : AbstractValidator<ScheduleConsolidateActorsCommand>
{
    public ScheduleConsolidateActorsCommandRuleSet()
    {
        RuleFor(command => command.FromActorId)
            .NotEmpty();

        RuleFor(command => command.Consolidation)
            .NotNull()
            .ChildRules(validator =>
            {
                validator.RuleFor(request => request.ToActorId)
                    .NotEmpty();

                validator.RuleFor(request => request.ConsolidateAt)
                    .GreaterThan(DateTimeOffset.UtcNow);

                // TODO: Implement once testing is done.
                // .Must(x => x.Day == 1); // Since mergers are always done at month end/start, we need to make sure the day is valid.
            });
    }
}
