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
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Contact;
using Energinet.DataHub.MarketParticipant.Application.Validation;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Validation
{
    [UnitTest]
    public sealed class CreateContactCommandRuleSetTests
    {
        private const string ValidCategory = "EnerginetInquiry";
        private const string ValidName = "John Doe";
        private const string ValidEmail = "john@example.com";
        private const string ValidPhone = "+45 01 02 03 04";

        private static readonly Guid _validOrganizationId = Guid.NewGuid();
        private static readonly string _test250Length = new('a', 250);
        private static readonly string _test251Length = new('a', 251);
        private static readonly string _test254Length = new('a', 254);
        private static readonly string _test255Length = new('a', 255);

        [Fact]
        public async Task Validate_OrganizationId_ValidatesProperty()
        {
            // Arrange
            const string propertyName = nameof(CreateContactCommand.OrganizationId);

            var target = new CreateContactCommandRuleSet();
            var command = new CreateContactCommand(Guid.Empty, new CreateContactDto(
                ValidName,
                ValidCategory,
                ValidEmail,
                ValidPhone));

            // Act
            var result = await target.ValidateAsync(command).ConfigureAwait(false);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
        }

        [Theory]
        [InlineData("", false)]
        [InlineData(null, false)]
        [InlineData("  ", false)]
        [InlineData("EnerginetInquiry", true)]
        [InlineData("Reminder", true)]
        [InlineData("NotAThing", false)]
        public async Task Validate_Category_ValidatesProperty(string value, bool isValid)
        {
            // Arrange
            var propertyName = $"{nameof(CreateContactCommand.Contact)}.{nameof(CreateContactDto.Category)}";

            var contactDto = new CreateContactDto(
                ValidName,
                value,
                ValidEmail,
                ValidPhone);

            var target = new CreateContactCommandRuleSet();
            var command = new CreateContactCommand(_validOrganizationId, contactDto);

            // Act
            var result = await target.ValidateAsync(command).ConfigureAwait(false);

            // Assert
            if (isValid)
            {
                Assert.True(result.IsValid);
                Assert.DoesNotContain(propertyName, result.Errors.Select(x => x.PropertyName));
            }
            else
            {
                Assert.False(result.IsValid);
                Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
            }
        }

        [Theory]
        [InlineData("", false)]
        [InlineData(null, false)]
        [InlineData("  ", false)]
        [InlineData("John Doe", true)]
        [InlineData("Reminder", true)]
        [InlineData("{max_value}", true)]
        [InlineData("{too_long_value}", false)]
        public async Task Validate_Name_ValidatesProperty(string value, bool isValid)
        {
            // Arrange
            var propertyName = $"{nameof(CreateContactCommand.Contact)}.{nameof(CreateContactDto.Name)}";

            if (value == "{max_value}")
                value = _test250Length;

            if (value == "{too_long_value}")
                value = _test251Length;

            var contactDto = new CreateContactDto(
                value,
                ValidCategory,
                ValidEmail,
                ValidPhone);

            var target = new CreateContactCommandRuleSet();
            var command = new CreateContactCommand(_validOrganizationId, contactDto);

            // Act
            var result = await target.ValidateAsync(command).ConfigureAwait(false);

            // Assert
            if (isValid)
            {
                Assert.True(result.IsValid);
                Assert.DoesNotContain(propertyName, result.Errors.Select(x => x.PropertyName));
            }
            else
            {
                Assert.False(result.IsValid);
                Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
            }
        }

        [Theory]
        [InlineData("", false)]
        [InlineData(null, false)]
        [InlineData("  ", false)]
        [InlineData("john@doe.com", true)]
        [InlineData("not_at_sign", false)]
        [InlineData("{max_value}", true)]
        [InlineData("{too_long_value}", false)]
        public async Task Validate_Email_ValidatesProperty(string value, bool isValid)
        {
            // Arrange
            var propertyName = $"{nameof(CreateContactCommand.Contact)}.{nameof(CreateContactDto.Email)}";

            if (value == "{max_value}")
                value = _test254Length;

            if (value == "{too_long_value}")
                value = _test255Length;

            var contactDto = new CreateContactDto(
                ValidName,
                ValidCategory,
                value,
                ValidPhone);

            var target = new CreateContactCommandRuleSet();
            var command = new CreateContactCommand(_validOrganizationId, contactDto);

            // Act
            var result = await target.ValidateAsync(command).ConfigureAwait(false);

            // Assert
            if (isValid)
            {
                Assert.True(result.IsValid);
                Assert.DoesNotContain(propertyName, result.Errors.Select(x => x.PropertyName));
            }
            else
            {
                Assert.False(result.IsValid);
                Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
            }
        }

        [Theory]
        [InlineData("", false)]
        [InlineData(null, true)]
        [InlineData("  ", false)]
        [InlineData("01020304", true)]
        [InlineData("+45 01020304", true)]
        [InlineData("01letters02", false)]
        [InlineData("101010101010101", true)]
        [InlineData("1010101010101010", false)]
        public async Task Validate_Phone_ValidatesProperty(string value, bool isValid)
        {
            // Arrange
            var propertyName = $"{nameof(CreateContactCommand.Contact)}.{nameof(CreateContactDto.Phone)}";

            var contactDto = new CreateContactDto(
                ValidName,
                ValidCategory,
                ValidEmail,
                value);

            var target = new CreateContactCommandRuleSet();
            var command = new CreateContactCommand(_validOrganizationId, contactDto);

            // Act
            var result = await target.ValidateAsync(command).ConfigureAwait(false);

            // Assert
            if (isValid)
            {
                Assert.True(result.IsValid);
                Assert.DoesNotContain(propertyName, result.Errors.Select(x => x.PropertyName));
            }
            else
            {
                Assert.False(result.IsValid);
                Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
            }
        }
    }
}
