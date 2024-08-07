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
using Energinet.DataHub.MarketParticipant.Application.Commands.Delegations;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using FluentValidation;

namespace Energinet.DataHub.MarketParticipant.Application.Validation;

public sealed class CreateProcessDelegationCommandRuleSet : AbstractValidator<CreateProcessDelegationCommand>
{
    public CreateProcessDelegationCommandRuleSet()
    {
        RuleFor(command => command.CreateDelegation)
            .NotNull()
            .ChildRules(validator =>
            {
                validator
                    .RuleFor(delegation => delegation.DelegatedFrom)
                    .NotEmpty();

                validator
                    .RuleFor(delegation => delegation.DelegatedTo)
                    .NotEmpty();

                validator
                    .RuleFor(delegation => delegation.GridAreas)
                    .NotEmpty();

                validator
                    .RuleForEach(delegation => delegation.GridAreas)
                    .NotEmpty();

                validator
                    .RuleFor(delegation => delegation.DelegatedProcesses)
                    .NotEmpty();

                validator
                    .RuleForEach(delegation => delegation.DelegatedProcesses)
                    .NotEmpty()
                    .Must(Enum.IsDefined);

                validator
                    .RuleFor(delegation => delegation.StartsAt)
                    .GreaterThanOrEqualTo(_ => Clock
                        .Instance
                        .GetCurrentInstant()
                        .InZone(Domain.Model.TimeZone.Dk)
                        .Date
                        .AtStartOfDayInZone(Domain.Model.TimeZone.Dk)
                        .ToDateTimeOffset());
            });
    }
}
