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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands;
using Energinet.DataHub.MarketParticipant.Application.Handlers.Email;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Model.Email;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Tests.Common;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Handlers
{
    [UnitTest]
    public sealed class SendEmailHandlerTests
    {
        private readonly UserInviteEmailTemplate _emailTemplate = new(new Dictionary<string, string>());

        [Fact]
        public async Task Handle_EmailEventFound_EmailIsSent()
        {
            // arrange
            var events = new[]
            {
                new EmailEvent(new MockedEmailAddress(), _emailTemplate)
            };

            var emailEventsRepositoryMock = new Mock<IEmailEventRepository>();
            emailEventsRepositoryMock.Setup(x => x.GetAllPendingEmailEventsAsync()).ReturnsAsync(events);

            var emailSenderMock = new Mock<IEmailSender>();

            var target = new SendEmailHandler(
                emailEventsRepositoryMock.Object,
                emailSenderMock.Object);

            // act
            await target.Handle(new SendEmailCommand(), CancellationToken.None);

            // assert
            emailSenderMock.Verify(c => c.SendEmailAsync(events[0].Email, events[0]), Times.Exactly(1));
            emailEventsRepositoryMock.Verify(c => c.MarkAsSentAsync(events[0]), Times.Exactly(1));
        }
    }
}
