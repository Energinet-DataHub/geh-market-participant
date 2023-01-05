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
using Energinet.DataHub.Core.App.Common.Security;
using Energinet.DataHub.MarketParticipant.Application.Commands.Contact;
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoles;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using FluentValidation;

namespace Energinet.DataHub.MarketParticipant.Application.Validation
{
    public sealed class CreateUserRoleCommandRuleSet : AbstractValidator<CreateUserRoleCommand>
    {
        public CreateUserRoleCommandRuleSet()
        {
            RuleFor(command => command.UserRoleDto)
                .NotNull()
                .ChildRules(validator =>
                {
                    validator
                        .RuleFor(role => role.Name)
                        .NotEmpty()
                        .Length(1, 250);

                    validator
                        .RuleFor(role => role.EicFunction)
                        .IsInEnum();

                    validator
                        .RuleFor(role => role.Status)
                        .IsInEnum();

                    validator
                        .RuleForEach(role => role.Permissions)
                        .IsEnumName(typeof(Permission), false);
                });
        }
    }
}
