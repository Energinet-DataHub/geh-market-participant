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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands;
using Energinet.DataHub.MarketParticipant.Application.Handlers.Email;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users.Authentication;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Tests.Common;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Handlers
{
    [UnitTest]
    public sealed class SendUserInviteEmailHandlerTests
    {
        [Fact]
        public async Task Handle_EmailEventFound_EmailsSent()
        {
            // arrange
            var events = new[]
            {
                new EmailEvent(new MockedEmailAddress(), EmailEventType.UserInvite)
            };

            var emailEventsRepositoryMock = new Mock<IEmailEventRepository>();
            emailEventsRepositoryMock.Setup(x => x.GetAllEmailsToBeSentByTypeAsync(EmailEventType.UserInvite)).ReturnsAsync(events);

            var userIdentity = new UserIdentity(
                new ExternalUserId(Guid.NewGuid()),
                events[0].Email,
                UserStatus.Active,
                "firstName",
                "lastName",
                new PhoneNumber("23232323"),
                DateTimeOffset.Now,
                AuthenticationMethod.Undetermined,
                new Mock<IList<LoginIdentity>>().Object);

            var userIdentityRepositoryMock = new Mock<IUserIdentityRepository>();
            userIdentityRepositoryMock.Setup(e => e.GetAsync(events[0].Email)).ReturnsAsync(userIdentity);

            var user = new User(new SharedUserReferenceId(), userIdentity.Id);

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(e => e.GetAsync(userIdentity.Id)).ReturnsAsync(user);

            var emailSenderMock = new Mock<IEmailSender>();

            var target = new SendUserInviteEmailHandler(
                userRepositoryMock.Object,
                emailEventsRepositoryMock.Object,
                emailSenderMock.Object,
                userIdentityRepositoryMock.Object);

            // act
            await target.Handle(new SendUserInviteEmailCommand(), CancellationToken.None).ConfigureAwait(false);

            // assert
            emailSenderMock.Verify(c => c.SendEmailAsync(events[0].Email, events[0]), Times.Exactly(1));
            emailEventsRepositoryMock.Verify(c => c.MarkAsSentAsync(events[0]), Times.Exactly(1));
        }

        [Fact]
        public async Task Handle_UserIdentityIsNull()
        {
            // arrange
            var events = new[]
            {
                new EmailEvent(new MockedEmailAddress(), EmailEventType.UserInvite)
            };

            var emailEventsRepositoryMock = new Mock<IEmailEventRepository>();
            emailEventsRepositoryMock.Setup(x => x.GetAllEmailsToBeSentByTypeAsync(EmailEventType.UserInvite)).ReturnsAsync(events);

            var userIdentityRepositoryMock = new Mock<IUserIdentityRepository>();
            userIdentityRepositoryMock.Setup(e => e.GetAsync(events[0].Email)).ReturnsAsync((UserIdentity?)null);

            var userRepositoryMock = new Mock<IUserRepository>();
            var emailSenderMock = new Mock<IEmailSender>();

            var target = new SendUserInviteEmailHandler(
                userRepositoryMock.Object,
                emailEventsRepositoryMock.Object,
                emailSenderMock.Object,
                userIdentityRepositoryMock.Object);

            // act + assert
            await Assert
                .ThrowsAsync<NotSupportedException>(() => target.Handle(new SendUserInviteEmailCommand(), CancellationToken.None))
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task Handle_UserIsNull()
        {
            // arrange
            var events = new[]
            {
                new EmailEvent(new MockedEmailAddress(), EmailEventType.UserInvite)
            };

            var emailEventsRepositoryMock = new Mock<IEmailEventRepository>();
            emailEventsRepositoryMock.Setup(x => x.GetAllEmailsToBeSentByTypeAsync(EmailEventType.UserInvite)).ReturnsAsync(events);

            var userIdentity = new UserIdentity(
                new ExternalUserId(Guid.NewGuid()),
                events[0].Email,
                UserStatus.Active,
                "firstName",
                "lastName",
                new PhoneNumber("23232323"),
                DateTimeOffset.Now,
                AuthenticationMethod.Undetermined,
                new Mock<IList<LoginIdentity>>().Object);

            var userIdentityRepositoryMock = new Mock<IUserIdentityRepository>();
            userIdentityRepositoryMock.Setup(e => e.GetAsync(events[0].Email)).ReturnsAsync(userIdentity);

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(e => e.GetAsync(userIdentity.Id)).ReturnsAsync((User?)null);

            var emailSenderMock = new Mock<IEmailSender>();

            var target = new SendUserInviteEmailHandler(
                userRepositoryMock.Object,
                emailEventsRepositoryMock.Object,
                emailSenderMock.Object,
                userIdentityRepositoryMock.Object);

            // act + assert
            await Assert
                .ThrowsAsync<NotSupportedException>(() => target.Handle(new SendUserInviteEmailCommand(), CancellationToken.None))
                .ConfigureAwait(false);
        }
    }
}
