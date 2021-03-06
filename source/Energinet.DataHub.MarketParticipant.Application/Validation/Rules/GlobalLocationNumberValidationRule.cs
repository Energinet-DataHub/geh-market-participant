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
using System.Globalization;
using System.Linq;
using Energinet.DataHub.MarketParticipant.Application.Commands.Actor;
using FluentValidation;
using FluentValidation.Validators;

namespace Energinet.DataHub.MarketParticipant.Application.Validation.Rules
{
    public sealed class GlobalLocationNumberValidationRule<T> : PropertyValidator<T, ActorNumberDto?>
    {
        public override string Name => "GlobalLocationNumberValidation";

        public override bool IsValid(ValidationContext<T> context, ActorNumberDto? value)
        {
            return !string.IsNullOrEmpty(value?.Value) && IsValidGlnNumber(value.Value);
        }

        protected override string GetDefaultMessageTemplate(string errorCode)
        {
            return "'{PropertyName}' must be a valid GLN or EIC.";
        }

        private static bool IsValidGlnNumber(string glnNumber)
        {
            return LengthIsValid(glnNumber) && AllCharsAreDigits(glnNumber) && CheckSumIsValid(glnNumber);
        }

        private static bool LengthIsValid(string glnNumber)
        {
            return glnNumber.Length == 13;
        }

        private static bool AllCharsAreDigits(string glnNumber)
        {
            return glnNumber.All(char.IsDigit);
        }

        private static bool CheckSumIsValid(string glnNumber)
        {
            var definedChecksumDigit = Parse(glnNumber[^1..]);
            var calculatedChecksum = CalculateChecksum(glnNumber);
            return calculatedChecksum == definedChecksumDigit;
        }

        private static int CalculateChecksum(string glnNumber)
        {
            var sumOfOddNumbers = 0;
            var sumOfEvenNumbers = 0;

            for (var i = 1; i < glnNumber.Length; i++)
            {
                var currentNumber = Parse(glnNumber[(i - 1)..i]);

                if (IsEvenNumber(i))
                    sumOfEvenNumbers += currentNumber;
                else
                    sumOfOddNumbers += currentNumber;
            }

            var sum = (sumOfEvenNumbers * 3) + sumOfOddNumbers;

            var equalOrHigherMultipleOf = (int)(Math.Ceiling(sum / 10.0) * 10);
            return equalOrHigherMultipleOf - sum;
        }

        private static int Parse(string input)
        {
            return int.Parse(input, NumberStyles.Integer, NumberFormatInfo.InvariantInfo);
        }

        private static bool IsEvenNumber(int index)
        {
            return index % 2 == 0;
        }
    }
}
