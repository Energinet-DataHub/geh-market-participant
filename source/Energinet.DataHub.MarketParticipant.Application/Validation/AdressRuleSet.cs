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

using Energinet.DataHub.MarketParticipant.Application.Commands.Organization;
using Energinet.DataHub.MarketParticipant.Client.Models;
using FluentValidation;

namespace Energinet.DataHub.MarketParticipant.Application.Validation
{
    public sealed class AdressRuleSet : AbstractValidator<AddressDto>
    {
        public AdressRuleSet()
        {
            RuleFor(address => address.City)
                .NotEmpty()
                .Length(1, 50)
                .When(address => !string.IsNullOrEmpty(address.City));

            RuleFor(address => address.Country).NotEmpty().Length(1, 50);

            RuleFor(address => address.Number)
                .NotEmpty()
                .Length(1, 15)
                .When(address => !string.IsNullOrEmpty(address.Number));

            RuleFor(address => address.StreetName)
                .NotEmpty()
                .Length(1, 250)
                .When(address => !string.IsNullOrEmpty(address.StreetName));

            RuleFor(address => address.ZipCode)
                .NotEmpty()
                .Length(1, 15)
                .When(address => !string.IsNullOrEmpty(address.ZipCode));
        }
    }
}
