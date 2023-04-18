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
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.User;
using Energinet.DataHub.MarketParticipant.Application.Handlers.User;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Handlers;

[UnitTest]
public sealed class UpdateUserIdentityHandlerTests
{
    [Fact]
    public async Task Completes_With_Success()
    {
        // Arrange
        var userIdentityRepository = new Mock<IUserIdentityRepository>();
        var userRepositoryMock = new Mock<IUserRepository>();

        var userIdentityUpdateDto = new UserIdentityUpdateDto("+45 23232323");
        var validUserId = Guid.NewGuid();

        var user = new User(new UserId(validUserId), new ExternalUserId(Guid.NewGuid()), new List<UserRoleAssignment>());
        userRepositoryMock.Setup(x => x.GetAsync(user.Id)).ReturnsAsync(user);

        var target = new UpdateUserIdentityHandler(
            userRepositoryMock.Object,
            userIdentityRepository.Object);

        var updateUserIdentityCommand = new UpdateUserIdentityCommand(userIdentityUpdateDto, validUserId);

        // Act
        var result = await target.Handle(updateUserIdentityCommand, default);

        // Assert
        Assert.Equal(MediatR.Unit.Value, result);
    }

    [Fact]
    public async Task Invalid_PhoneNumber_ValidationException()
    {
        // Arrange
        var userIdentityRepository = new Mock<IUserIdentityRepository>();
        var userRepositoryMock = new Mock<IUserRepository>();

        var userIdentityUpdateDto = new UserIdentityUpdateDto("+45 invalid");
        var validUserId = Guid.NewGuid();

        var target = new UpdateUserIdentityHandler(
            userRepositoryMock.Object,
            userIdentityRepository.Object);

        var updateUserIdentityCommand = new UpdateUserIdentityCommand(userIdentityUpdateDto, validUserId);

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(() => target.Handle(updateUserIdentityCommand, default)).ConfigureAwait(false);
    }

    [Fact]
    public async Task UserNotFound_Throws()
    {
        // Arrange
        var userIdentityRepository = new Mock<IUserIdentityRepository>();
        var userRepositoryMock = new Mock<IUserRepository>();

        var userIdentityUpdateDto = new UserIdentityUpdateDto("+45 23232323");
        var validUserId = Guid.NewGuid();

        var target = new UpdateUserIdentityHandler(
            userRepositoryMock.Object,
            userIdentityRepository.Object);

        var updateUserIdentityCommand = new UpdateUserIdentityCommand(userIdentityUpdateDto, validUserId);

        // Act + Assert
        await Assert.ThrowsAsync<NotFoundValidationException>(() => target.Handle(updateUserIdentityCommand, default)).ConfigureAwait(false);
    }
}
