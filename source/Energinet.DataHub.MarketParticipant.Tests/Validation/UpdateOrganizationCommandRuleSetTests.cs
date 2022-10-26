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
using Energinet.DataHub.MarketParticipant.Application.Commands.Organization;
using Energinet.DataHub.MarketParticipant.Application.Validation;
using Xunit;
using Xunit.Categories;
using ClientModels = Energinet.DataHub.MarketParticipant.Client.Models;

namespace Energinet.DataHub.MarketParticipant.Tests.Validation
{
    [UnitTest]
    public sealed class UpdateOrganizationCommandRuleSetTests
    {
        private const string ValidName = "Company Name";
        private const string ValidCvr = "12345678";

        private static readonly Guid _validOrganizationId = Guid.NewGuid();

        [Fact]
        public async Task Validate_OrganizationId_ValidatesProperty()
        {
            // Arrange
            const string propertyName = nameof(UpdateOrganizationCommand.OrganizationId);

            var organizationDto = new ClientModels.ChangeOrganizationDto(
                ValidName,
                string.Empty,
                new ClientModels.AddressDto(
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty),
                "fake_value",
                ClientModels.OrganizationStatus.Active);

            var target = new UpdateOrganizationCommandRuleSet();
            var command = new UpdateOrganizationCommand(Guid.Empty, organizationDto);

            // Act
            var result = await target.ValidateAsync(command).ConfigureAwait(false);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
        }

        [Fact]
        public async Task Validate_OrganizationDto_ValidatesProperty()
        {
            // Arrange
            const string propertyName = nameof(UpdateOrganizationCommand.Organization);

            var target = new UpdateOrganizationCommandRuleSet();
            var command = new UpdateOrganizationCommand(_validOrganizationId, null!);

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
        [InlineData("Some Name", true)]
        [InlineData("Maximum looooooooooooooooooooooooooooooooooooooong", true)]
        [InlineData("Toooooo loooooooooooooooooooooooooooooooooooooooong", false)]
        public async Task Validate_OrganizationName_ValidatesProperty(string value, bool isValid)
        {
            // Arrange
            var propertyName = $"{nameof(UpdateOrganizationCommand.Organization)}.{nameof(ClientModels.ChangeOrganizationDto.Name)}";

            var organizationDto = new ClientModels.ChangeOrganizationDto(
                value,
                ValidCvr,
                new ClientModels.AddressDto(
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    "Denmark"),
                "fake_value",
                ClientModels.OrganizationStatus.Active);

            var target = new UpdateOrganizationCommandRuleSet();
            var command = new UpdateOrganizationCommand(_validOrganizationId, organizationDto);

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
        [InlineData(ClientModels.OrganizationStatus.New, true)]
        [InlineData(ClientModels.OrganizationStatus.Active, true)]
        [InlineData(ClientModels.OrganizationStatus.Blocked, true)]
        [InlineData(ClientModels.OrganizationStatus.Deleted, true)]
        public async Task Validate_OrganizationStatus_ValidatesProperty(ClientModels.OrganizationStatus status, bool isValid)
        {
            // Arrange
            const string propertyName = $"{nameof(UpdateOrganizationCommand.Organization)}.{nameof(ClientModels.ChangeOrganizationDto.Status)}";

            var organizationDto = new ClientModels.ChangeOrganizationDto(
                "fake_value",
                ValidCvr,
                new ClientModels.AddressDto(
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    "Denmark"),
                "fake_value",
                status);

            var target = new UpdateOrganizationCommandRuleSet();
            var command = new UpdateOrganizationCommand(_validOrganizationId, organizationDto);

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
        [InlineData("12345678", true)]
        [InlineData("123456789", false)]
        public async Task Validate_OrganizationBusinessRegisterIdentifier_ValidatesProperty(string value, bool isValid)
        {
            // Arrange
            var propertyName = $"{nameof(UpdateOrganizationCommand.Organization)}.{nameof(ClientModels.ChangeOrganizationDto.BusinessRegisterIdentifier)}";

            var organizationDto = new ClientModels.ChangeOrganizationDto(
                ValidName,
                value,
                new ClientModels.AddressDto(
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    "Denmark"),
                "fake_value",
                ClientModels.OrganizationStatus.Active);

            var target = new UpdateOrganizationCommandRuleSet();
            var command = new UpdateOrganizationCommand(_validOrganizationId, organizationDto);

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
        [InlineData("", true)]
        [InlineData(null, true)]
        [InlineData("  ", false)]
        [InlineData("Np81mDz09UmzLJphrnvA2Suzm5GItyOjb0sWJgXhAasF9vFceybMCicq3kT1O4JWD0PKXzPjtK8QUQwVUUHo4HV3zePz5eXZXYfGz2Zvr9tpMLsxKv6TiDPhX27g1IUzAUyoRJbKW65uZVlS2N2JbthH3uKEYnmhe3O14z2VDDKLnXbMU7uqfQ8XyAIXOPEz2jEnft8sGXSqDB7hw2njPTILlboBqohahxXdS0YfB4FFoR55wp9xdG0ULO", true)]
        [InlineData("Np81mDz09UmzLJphrnvA2Suzm5GItyOjb0sWJgXhAasF9vFceybMCicq3kT1O4JWD0PKXzPjtK8QUQwVUUHo4HV3zePz5eXZXYfGz2Zvr9tpMLsxKv6TiDPhX27g1IUzAUyoRJbKW65uZVlS2N2JbthH3uKEYnmhe3O14z2VDDKLnXbMU7uqfQ8XyAIXOPEz2jEnft8sGXSqDB7hw2njPTILlboBqohahxXdS0YfB4FFoR55wp9xdG0ULOa", false)]
        public async Task Validate_OrganizationAddressStreetname_ValidatesProperty(string value, bool isValid)
        {
            // Arrange
            var propertyName = $"{nameof(UpdateOrganizationCommand.Organization)}.{nameof(ClientModels.ChangeOrganizationDto.Address)}.{nameof(ClientModels.ChangeOrganizationDto.Address.StreetName)}";

            var organizationDto = new ClientModels.ChangeOrganizationDto(
                ValidName,
                ValidCvr,
                new ClientModels.AddressDto(
                    value,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    "Denmark"),
                "fake_value",
                ClientModels.OrganizationStatus.Active);

            var target = new UpdateOrganizationCommandRuleSet();
            var command = new UpdateOrganizationCommand(_validOrganizationId, organizationDto);

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
        [InlineData("", true)]
        [InlineData(null, true)]
        [InlineData("  ", false)]
        [InlineData("I5eel4D4bPnbdEi2O9lQNggj1vjXQPdyhsebRVpqMCPVCevBX2", true)]
        [InlineData("I5eel4D4bPnbdEi2O9lQNggj1vjXQPdyhsebRVpqMCPVCevBX2A", false)]
        public async Task Validate_OrganizationAddressCity_ValidatesProperty(string value, bool isValid)
        {
            // Arrange
            var propertyName = $"{nameof(UpdateOrganizationCommand.Organization)}.{nameof(ClientModels.ChangeOrganizationDto.Address)}.{nameof(ClientModels.ChangeOrganizationDto.Address.City)}";

            var organizationDto = new ClientModels.ChangeOrganizationDto(
                ValidName,
                ValidCvr,
                new ClientModels.AddressDto(
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    value,
                    "Denmark"),
                "fake_value",
                ClientModels.OrganizationStatus.Active);

            var target = new UpdateOrganizationCommandRuleSet();
            var command = new UpdateOrganizationCommand(_validOrganizationId, organizationDto);

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
        [InlineData("I5eel4D4bPnbdEi2O9lQNggj1vjXQPdyhsebRVpqMCPVCevBX2", true)]
        [InlineData("I5eel4D4bPnbdEi2O9lQNggj1vjXQPdyhsebRVpqMCPVCevBX2A", false)]
        public async Task Validate_OrganizationAddressCountry_ValidatesProperty(string value, bool isValid)
        {
            // Arrange
            var propertyName = $"{nameof(UpdateOrganizationCommand.Organization)}.{nameof(ClientModels.ChangeOrganizationDto.Address)}.{nameof(ClientModels.ChangeOrganizationDto.Address.Country)}";

            var organizationDto = new ClientModels.ChangeOrganizationDto(
                ValidName,
                ValidCvr,
                new ClientModels.AddressDto(
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    value),
                "fake_value",
                ClientModels.OrganizationStatus.Active);

            var target = new UpdateOrganizationCommandRuleSet();
            var command = new UpdateOrganizationCommand(_validOrganizationId, organizationDto);

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
        [InlineData("", true)]
        [InlineData(null, true)]
        [InlineData("  ", false)]
        [InlineData("K57S9FHJZjmhB6U", true)]
        [InlineData("K57S9FHJZjmhB6UA", false)]
        public async Task Validate_OrganizationAddressNumber_ValidatesProperty(string value, bool isValid)
        {
            // Arrange
            var propertyName = $"{nameof(UpdateOrganizationCommand.Organization)}.{nameof(ClientModels.ChangeOrganizationDto.Address)}.{nameof(ClientModels.ChangeOrganizationDto.Address.Number)}";

            var organizationDto = new ClientModels.ChangeOrganizationDto(
                ValidName,
                ValidCvr,
                new ClientModels.AddressDto(
                    string.Empty,
                    value,
                    string.Empty,
                    string.Empty,
                    "Denmark"),
                "fake_value",
                ClientModels.OrganizationStatus.Active);

            var target = new UpdateOrganizationCommandRuleSet();
            var command = new UpdateOrganizationCommand(_validOrganizationId, organizationDto);

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
        [InlineData("", true)]
        [InlineData(null, true)]
        [InlineData("  ", false)]
        [InlineData("K57S9FHJZjmhB6U", true)]
        [InlineData("K57S9FHJZjmhB6UA", false)]
        public async Task Validate_OrganizationAddressZipCode_ValidatesProperty(string value, bool isValid)
        {
            // Arrange
            var propertyName = $"{nameof(UpdateOrganizationCommand.Organization)}.{nameof(ClientModels.ChangeOrganizationDto.Address)}.{nameof(ClientModels.ChangeOrganizationDto.Address.ZipCode)}";

            var organizationDto = new ClientModels.ChangeOrganizationDto(
                ValidName,
                ValidCvr,
                new ClientModels.AddressDto(
                    string.Empty,
                    string.Empty,
                    value,
                    string.Empty,
                    "Denmark"),
                "fake_value",
                ClientModels.OrganizationStatus.Active);

            var target = new UpdateOrganizationCommandRuleSet();
            var command = new UpdateOrganizationCommand(_validOrganizationId, organizationDto);

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
