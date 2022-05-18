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
using System.Collections.Generic;
using System.Linq;
using Energinet.DataHub.MarketParticipant.Application.Commands.Actor;
using Energinet.DataHub.MarketParticipant.Application.Validation.EicFunctionGroups;
using FluentValidation;
using FluentValidation.Validators;

namespace Energinet.DataHub.MarketParticipant.Application.Validation.Rules
{
    public sealed class RolesValidationRule<T> : PropertyValidator<T, IEnumerable<MarketRoleDto>>
    {
        public override string Name => "RolesValidation";

        public override bool IsValid(ValidationContext<T> context, IEnumerable<MarketRoleDto> value)
        {
            ArgumentNullException.ThrowIfNull(value);

            List<bool> isContained = new();

            if (value.All(x => DdkDdqMdr.Roles.Contains(x.Function)))
            {
                isContained.Add(true);
            }

            if (value.All(x => DdmMdr.Roles.Contains(x.Function)))
            {
                isContained.Add(true);
            }

            if (value.All(x => Ddx.Roles.Contains(x.Function)))
            {
                isContained.Add(true);
            }

            if (value.All(x => Ddz.Roles.Contains(x.Function)))
            {
                isContained.Add(true);
            }

            if (value.All(x => Dgl.Roles.Contains(x.Function)))
            {
                isContained.Add(true);
            }

            if (value.All(x => Ez.Roles.Contains(x.Function)))
            {
                isContained.Add(true);
            }

            return isContained.Count <= 1;
        }

        protected override string GetDefaultMessageTemplate(string errorCode)
        {
            return "'{PropertyName}' Roles across role groups have been chosen. Please choose roles only contained in one group.";
        }
    }
}
