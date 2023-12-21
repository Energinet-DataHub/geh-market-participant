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
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users.Authentication;
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
        var auditIdentityProviderMock = new Mock<IAuditIdentityProvider>();
        var userIdentityRepository = new Mock<IUserIdentityRepository>();
        var userRepositoryMock = new Mock<IUserRepository>();
        var userIdentityAuditLogEntryRepository = new Mock<IUserIdentityAuditLogRepository>();

        auditIdentityProviderMock
            .Setup(x => x.IdentityId)
            .Returns(new AuditIdentity(Guid.NewGuid()));

        var userIdentityUpdateDto = new UserIdentityUpdateDto("firstName", "lastName", "+45 23232323");
        var validUserId = Guid.NewGuid();

        var user = CreateFakeUser(validUserId);
        userRepositoryMock
            .Setup(x => x.GetAsync(user.Id))
            .ReturnsAsync(user);

        userIdentityRepository
            .Setup(x => x.GetAsync(user.ExternalId))
            .ReturnsAsync(CreateFakeUserIdentity);

        var target = new UpdateUserIdentityHandler(
            auditIdentityProviderMock.Object,
            userRepositoryMock.Object,
            userIdentityRepository.Object,
            userIdentityAuditLogEntryRepository.Object,
            UnitOfWorkProviderMock.Create());

        var updateUserIdentityCommand = new UpdateUserIdentityCommand(userIdentityUpdateDto, validUserId);

        // Act + Assert
        await target.Handle(updateUserIdentityCommand, default);
    }

    [Fact]
    public async Task Invalid_PhoneNumber_ValidationException()
    {
        // Arrange
        var auditIdentityProviderMock = new Mock<IAuditIdentityProvider>();
        auditIdentityProviderMock
            .Setup(x => x.IdentityId)
            .Returns(new AuditIdentity(Guid.NewGuid()));

        var userRepositoryMock = new Mock<IUserRepository>();
        userRepositoryMock
            .Setup(x => x.GetAsync(It.IsAny<UserId>()))
            .ReturnsAsync(CreateFakeUser(Guid.NewGuid()));

        var userIdentityRepository = new Mock<IUserIdentityRepository>();
        userIdentityRepository
            .Setup(x => x.GetAsync(It.IsAny<ExternalUserId>()))
            .ReturnsAsync(CreateFakeUserIdentity());

        var userIdentityAuditLogEntryRepository = new Mock<IUserIdentityAuditLogRepository>();

        var userIdentityUpdateDto = new UserIdentityUpdateDto("firstName", "lastName", "+45 invalid");
        var validUserId = Guid.NewGuid();

        var target = new UpdateUserIdentityHandler(
            auditIdentityProviderMock.Object,
            userRepositoryMock.Object,
            userIdentityRepository.Object,
            userIdentityAuditLogEntryRepository.Object,
            UnitOfWorkProviderMock.Create());

        var updateUserIdentityCommand = new UpdateUserIdentityCommand(userIdentityUpdateDto, validUserId);

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(() => target.Handle(updateUserIdentityCommand, default));
    }

    [Fact]
    public async Task UserNotFound_Throws()
    {
        // Arrange
        var auditIdentityProviderMock = new Mock<IAuditIdentityProvider>();
        var userIdentityRepository = new Mock<IUserIdentityRepository>();
        var userRepositoryMock = new Mock<IUserRepository>();
        var userIdentityAuditLogEntryRepository = new Mock<IUserIdentityAuditLogRepository>();

        auditIdentityProviderMock
            .Setup(x => x.IdentityId)
            .Returns(new AuditIdentity(Guid.NewGuid()));

        var userIdentityUpdateDto = new UserIdentityUpdateDto("firstName", "lastName", "+45 23232324");
        var validUserId = Guid.NewGuid();

        var target = new UpdateUserIdentityHandler(
            auditIdentityProviderMock.Object,
            userRepositoryMock.Object,
            userIdentityRepository.Object,
            userIdentityAuditLogEntryRepository.Object,
            UnitOfWorkProviderMock.Create());

        var updateUserIdentityCommand = new UpdateUserIdentityCommand(userIdentityUpdateDto, validUserId);

        // Act + Assert
        await Assert.ThrowsAsync<NotFoundValidationException>(() => target.Handle(updateUserIdentityCommand, default));
    }

    private static UserIdentity CreateFakeUserIdentity() => new(
        new SharedUserReferenceId(),
        new EmailAddress("fake@example.dk"),
        "first",
        "last",
        new PhoneNumber("+45 23232323"),
        new SmsAuthenticationMethod(new PhoneNumber("+45 23232323")));

    private static User CreateFakeUser(Guid userId) => new(
        new UserId(userId),
        new ActorId(Guid.Empty),
        new ExternalUserId(Guid.NewGuid()),
        new List<UserRoleAssignment>(),
        null,
        null);
}
