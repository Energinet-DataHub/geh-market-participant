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

using System.Linq;
using Energinet.DataHub.MarketParticipant.Application.Commands.Actor;
using FluentValidation;
using FluentValidation.Validators;

namespace Energinet.DataHub.MarketParticipant.Application.Validation.Rules
{
    /// <summary>
    /// EIC Validation
    /// The EIC is based on fixed length alphanumeric codes which can be broken down as follows:
    /// A 2-character number identifying the Issuing Office assigned by ENTSO-E.
    /// One Character identifying the object type that the code represents.
    /// 12 digits, uppercase characters or minus signs allocated by the issuing office
    /// 1 check character to ensure the code validity.
    /// Valid characters of an EIC code are A-Z, 0-9 and “-“.
    /// </summary>
    public sealed class EnergyIdentificationCodeValidationRule<T> : PropertyValidator<T, ActorNumberDto?>
    {
        public override string Name => "EnergyIdentificationCodeValidation";

        public override bool IsValid(ValidationContext<T> context, ActorNumberDto? value)
        {
            return !string.IsNullOrEmpty(value?.Value) && IsValidEic(value.Value);
        }

        protected override string GetDefaultMessageTemplate(string errorCode)
        {
            return "'{PropertyName}' must be a valid GLN or EIC.";
        }

        private static bool IsValidEic(string energyIdentificationCode)
        {
            return LengthIsValid(energyIdentificationCode)
                   && ValidateTwoCharacterIssuingNumber(energyIdentificationCode)
                   && ValidateObjectTypeCharacter(energyIdentificationCode)
                   && ValidateTwelveDigitsUpperCaseCharacters(energyIdentificationCode)
                   && ValidateCheckCharacter(energyIdentificationCode);
        }

        private static bool LengthIsValid(string energyIdentificationCode)
        {
            return energyIdentificationCode.Length == 16;
        }

        private static bool ValidateTwoCharacterIssuingNumber(string energyIdentificationCode)
        {
            return energyIdentificationCode[..2].All(char.IsNumber);
        }

        private static bool ValidateObjectTypeCharacter(string energyIdentificationCode)
        {
            return energyIdentificationCode.Substring(2, 1).All(char.IsLetter);
        }

        private static bool ValidateTwelveDigitsUpperCaseCharacters(string energyIdentificationCode)
        {
            return energyIdentificationCode
                .Substring(3, 12)
                .All(c =>
                    c is '-'
                    || char.IsDigit(c)
                    || (char.IsLetterOrDigit(c) && char.IsUpper(c)));
        }

        private static bool ValidateCheckCharacter(string energyIdentificationCode)
        {
            return char.IsLetterOrDigit(energyIdentificationCode[^1]);
        }
    }
}
