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

using Energinet.DataHub.MarketParticipant.Application.Commands.Actor;
using Energinet.DataHub.MarketParticipant.Application.Validation.Rules;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using FluentValidation;

namespace Energinet.DataHub.MarketParticipant.Application.Validation
{
    public sealed class CreateActorCommandRuleSet : AbstractValidator<CreateActorCommand>
    {
        public CreateActorCommandRuleSet()
        {
            RuleFor(command => command.OrganizationId)
                .NotEmpty();

            RuleFor(command => command.Actor)
                .NotNull()
                .ChildRules(validator =>
                {
                    validator
                        .RuleFor(actor => actor.Gln)
                        .SetValidator(new GlobalLocationNumberValidationRule<CreateActorDto>())
                        .When(i => string.IsNullOrWhiteSpace(i.Gln.Value) || i.Gln.Value.Length <= 13);

                    validator
                        .RuleFor(actor => actor.Gln)
                        .SetValidator(new EnergyIdentificationCodeValidationRule<CreateActorDto>())
                        .When(i => string.IsNullOrWhiteSpace(i.Gln.Value) || i.Gln.Value.Length >= 14);

                    validator
                        .RuleFor(x => x.GridAreas)
                        .ChildRules(gridAreaValidator =>
                            gridAreaValidator
                                .RuleForEach(x => x)
                                .NotEmpty());

                    validator
                        .RuleFor(actor => actor.MarketRoles)
                        .NotEmpty()
                        .ChildRules(rolesValidator =>
                        {
                            rolesValidator
                                .RuleForEach(x => x)
                                .NotNull()
                                .ChildRules(roleValidator =>
                                {
                                    roleValidator
                                        .RuleFor(x => x.EicFunction)
                                        .NotEmpty()
                                        .IsEnumName(typeof(EicFunction), false);
                                });
                        });

                    validator
                        .RuleFor(actor => actor.MeteringPointTypes)
                        .NotEmpty()
                        .ChildRules(rolesValidator =>
                        {
                            rolesValidator
                                .RuleForEach(x => x)
                                .Must(x => MeteringPointType.TryFromName(x, true, out _));
                        });
                });
        }
    }
}
