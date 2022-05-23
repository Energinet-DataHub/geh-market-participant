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
using Energinet.DataHub.MarketParticipant.Domain.Model;
using FluentValidation;

namespace Energinet.DataHub.MarketParticipant.Application.Validation
{
    public sealed class UpdateActorCommandRuleSet : AbstractValidator<UpdateActorCommand>
    {
        public UpdateActorCommandRuleSet()
        {
            RuleFor(command => command.OrganizationId)
                .NotEmpty();

            RuleFor(command => command.ActorId)
                .NotEmpty();

            RuleFor(actor => actor.ChangeActor)
                .NotNull()
                .ChildRules(changeActorValidator =>
                {
                    changeActorValidator
                        .RuleFor(x => x.Status)
                        .NotEmpty()
                        .IsEnumName(typeof(ActorStatus), false);

                    changeActorValidator
                        .RuleFor(x => x.GridAreas)
                        .ChildRules(gridAreaValidator =>
                            gridAreaValidator
                                .RuleForEach(x => x)
                                .NotEmpty());

                    changeActorValidator
                        .RuleFor(x => x.MarketRoles)
                        .NotEmpty()
                        .ChildRules(rolesValidator =>
                            rolesValidator
                                .RuleForEach(x => x)
                                .NotNull()
                                .ChildRules(roleValidator =>
                                {
                                    roleValidator
                                        .RuleFor(x => x.EicFunction)
                                        .NotEmpty()
                                        .IsEnumName(typeof(EicFunction), false);
                                }));

                    changeActorValidator
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
