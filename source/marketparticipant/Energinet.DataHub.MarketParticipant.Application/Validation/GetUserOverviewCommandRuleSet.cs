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

using Energinet.DataHub.MarketParticipant.Application.Commands.Query.Users;
using FluentValidation;

namespace Energinet.DataHub.MarketParticipant.Application.Validation;

public sealed class GetUserOverviewCommandRuleSet : AbstractValidator<GetUserOverviewCommand>
{
    public GetUserOverviewCommandRuleSet()
    {
        RuleFor(command => command.Filter)
            .NotNull();

        RuleFor(command => command.Filter)
            .ChildRules(filterDtoRules =>
            {
                filterDtoRules
                    .RuleFor(filterDto => filterDto.ActorId)
                    .NotEmpty()
                    .When(filterDto => filterDto.ActorId.HasValue);

                filterDtoRules
                    .RuleForEach(x => x.UserStatus)
                    .IsInEnum();

                filterDtoRules
                    .RuleForEach(x => x.UserRoleIds)
                    .NotEmpty();
            });

        RuleFor(command => command.PageNumber)
            .GreaterThan(0);

        RuleFor(command => command.PageSize)
            .GreaterThan(0);
    }
}
