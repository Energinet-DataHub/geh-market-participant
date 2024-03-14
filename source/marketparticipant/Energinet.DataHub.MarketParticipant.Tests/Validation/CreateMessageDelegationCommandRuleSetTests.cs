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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Actor;
using Energinet.DataHub.MarketParticipant.Application.Commands.Delegations;
using Energinet.DataHub.MarketParticipant.Application.Validation;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Delegations;
using Xunit;
using Xunit.Categories;
namespace Energinet.DataHub.MarketParticipant.Tests.Validation
{
    [UnitTest]
    public sealed class CreateMessageDelegationCommandRuleSetTests
    {
        [Fact]
        public async Task Validate_CreateMessageDelegationDto_ValidatesProperty()
        {
            // Arrange
            const string propertyName = nameof(CreateMessageDelegationCommand.CreateDelegation);

            var target = new CreateMessageDelegationCommandRuleSet();
            var command = new CreateMessageDelegationCommand(null!);

            // Act
            var result = await target.ValidateAsync(command);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
        }

        [Theory]
        [InlineData("8F9B8218-BAE6-412B-B91B-0C78A55FF128", true)]
        [InlineData("00000000-0000-0000-0000-000000000000", false)]
        public async Task Validate_DelegatedFrom_ValidatesProperty(string value, bool isValid)
        {
            // Arrange
            const string propertyName = $"{nameof(CreateMessageDelegationCommand.CreateDelegation)}.{nameof(CreateMessageDelegationDto.DelegatedFrom)}";
            var gridAreaList = new List<GridAreaId>() { new GridAreaId(Guid.NewGuid()) };
            var messageTypeList = new List<DelegationMessageType>() { DelegationMessageType.Rsm012Inbound };
            var delegationDto = new CreateMessageDelegationDto(
                new ActorId(Guid.Parse(value)),
                new ActorId(Guid.NewGuid()),
                gridAreaList,
                messageTypeList,
                DateTimeOffset.UtcNow);

            var target = new CreateMessageDelegationCommandRuleSet();
            var command = new CreateMessageDelegationCommand(delegationDto);

            // Act
            var result = await target.ValidateAsync(command);

            // Assert
            if (isValid)
            {
                Assert.DoesNotContain(propertyName, result.Errors.Select(x => x.PropertyName));
                Assert.True(result.IsValid);
            }
            else
            {
                Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
                Assert.False(result.IsValid);
            }
        }

        [Theory]
        [InlineData("8F9B8218-BAE6-412B-B91B-0C78A55FF128", true)]
        [InlineData("00000000-0000-0000-0000-000000000000", false)]
        public async Task Validate_DelegatedTo_ValidatesProperty(string value, bool isValid)
        {
            // Arrange
            const string propertyName = $"{nameof(CreateMessageDelegationCommand.CreateDelegation)}.{nameof(CreateMessageDelegationDto.DelegatedTo)}";
            var gridAreaList = new List<GridAreaId>() { new GridAreaId(Guid.NewGuid()) };
            var messageTypeList = new List<DelegationMessageType>() { DelegationMessageType.Rsm012Inbound };
            var delegationDto = new CreateMessageDelegationDto(
                new ActorId(Guid.NewGuid()),
                new ActorId(Guid.Parse(value)),
                gridAreaList,
                messageTypeList,
                DateTimeOffset.UtcNow);

            var target = new CreateMessageDelegationCommandRuleSet();
            var command = new CreateMessageDelegationCommand(delegationDto);

            // Act
            var result = await target.ValidateAsync(command);

            // Assert
            if (isValid)
            {
                Assert.DoesNotContain(propertyName, result.Errors.Select(x => x.PropertyName));
                Assert.True(result.IsValid);
            }
            else
            {
                Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
                Assert.False(result.IsValid);
            }
        }

        [Theory]
        [InlineData(new[] { "8F9B8218-BAE6-412B-B91B-0C78A55FF128", "8F9B8218-BAE6-412B-B91B-0C78A55FF122" }, true)]
        [InlineData(new[] { "00000000-0000-0000-0000-000000000000", "8F9B8218-BAE6-412B-B91B-0C78A55FF128" }, false)]
        [InlineData(new[] { "8F9B8218-BAE6-412B-B91B-0C78A55FF128", "00000000-0000-0000-0000-000000000000" }, false)]
        [InlineData(new string[] { }, false)]
        public async Task Validate_GridAreas_ValidatesProperty(string[] value, bool isValid)
        {
            // Arrange
            const string propertyName = $"{nameof(CreateMessageDelegationCommand.CreateDelegation)}.{nameof(CreateMessageDelegationDto.GridAreas)}";
            var gridAreaList = value.Select(x => new GridAreaId(Guid.Parse(x))).ToList();
            var messageTypeList = new List<DelegationMessageType>() { DelegationMessageType.Rsm012Inbound };
            var delegationDto = new CreateMessageDelegationDto(
                new ActorId(Guid.NewGuid()),
                new ActorId(Guid.NewGuid()),
                gridAreaList,
                messageTypeList,
                DateTimeOffset.UtcNow);

            var target = new CreateMessageDelegationCommandRuleSet();
            var command = new CreateMessageDelegationCommand(delegationDto);

            // Act
            var result = await target.ValidateAsync(command);

            // Assert
            if (isValid)
            {
                Assert.DoesNotContain(result.Errors, x => x.PropertyName.StartsWith(propertyName, StringComparison.OrdinalIgnoreCase));
                Assert.True(result.IsValid);
            }
            else
            {
                Assert.Contains(result.Errors, x => x.PropertyName.StartsWith(propertyName, StringComparison.OrdinalIgnoreCase));
                Assert.False(result.IsValid);
            }
        }

        [Theory]
        [InlineData(new[] { DelegationMessageType.Rsm012Inbound, DelegationMessageType.Rsm014Inbound }, true)]
        [InlineData(new[] { DelegationMessageType.Rsm012Inbound, (DelegationMessageType)550 }, false)]
        [InlineData(new DelegationMessageType[] { }, false)]
        public async Task Validate_MessagesTypes_ValidatesProperty(DelegationMessageType[] value, bool isValid)
        {
            // Arrange
            const string propertyName = $"{nameof(CreateMessageDelegationCommand.CreateDelegation)}.{nameof(CreateMessageDelegationDto.MessageTypes)}";
            var gridAreaList = new List<GridAreaId>() { new GridAreaId(Guid.NewGuid()) };
            var messageTypeList = new List<DelegationMessageType>(value);
            var delegationDto = new CreateMessageDelegationDto(
                new ActorId(Guid.NewGuid()),
                new ActorId(Guid.NewGuid()),
                gridAreaList,
                messageTypeList,
                DateTimeOffset.UtcNow);

            var target = new CreateMessageDelegationCommandRuleSet();
            var command = new CreateMessageDelegationCommand(delegationDto);

            // Act
            var result = await target.ValidateAsync(command);

            // Assert
            if (isValid)
            {
                Assert.DoesNotContain(result.Errors, x => x.PropertyName.StartsWith(propertyName, StringComparison.OrdinalIgnoreCase));
                Assert.True(result.IsValid);
            }
            else
            {
                Assert.Contains(result.Errors, x => x.PropertyName.StartsWith(propertyName, StringComparison.OrdinalIgnoreCase));
                Assert.False(result.IsValid);
            }
        }
    }
}
