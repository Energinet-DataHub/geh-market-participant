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

using System.Linq;
using Energinet.DataHub.MarketParticipant.Application.Commands.Organizations;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using FluentValidation;

namespace Energinet.DataHub.MarketParticipant.Application.Validation;

public sealed class UpdateOrganizationCommandRuleSet : AbstractValidator<UpdateOrganizationCommand>
{
    public UpdateOrganizationCommandRuleSet()
    {
        RuleFor(command => command.OrganizationId)
            .NotEmpty();

        RuleFor(command => command.Organization)
            .NotNull()
            .ChildRules(validator =>
            {
                validator
                    .RuleFor(organization => organization.Name)
                    .NotEmpty()
                    .Length(1, 50);

                validator
                    .RuleFor(organization => organization.Status)
                    .NotEmpty()
                    .IsEnumName(typeof(OrganizationStatus));

                validator
                    .RuleFor(organization => organization.Domains)
                    .Must(domains => domains.All(OrganizationDomain.IsValid));
            });
    }
}
