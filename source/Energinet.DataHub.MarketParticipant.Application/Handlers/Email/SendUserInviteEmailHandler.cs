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

using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Email
{
    public sealed class SendUserInviteEmailHandler : IRequestHandler<SendUserInviteEmailCommand, Unit>
    {
        private readonly IUserRepository _userRepository;
        private readonly IEmailEventRepository _emailEventRepository;
        private readonly IEmailSender _gridEmailSenderSender;

        public SendUserInviteEmailHandler(
            IUserRepository userRepository,
            IEmailEventRepository emailEventRepository,
            IEmailSender gridEmailSenderSender)
        {
            _userRepository = userRepository;
            _emailEventRepository = emailEventRepository;
            _gridEmailSenderSender = gridEmailSenderSender;
        }

        public async Task<Unit> Handle(SendUserInviteEmailCommand request, CancellationToken cancellationToken)
        {
            // Find email event to be sent
            var invitesToBeSent = await _emailEventRepository
                .GetAllEmailsToBeSentByTypeAsync(EmailEventType.UserInvite)
                .ConfigureAwait(false);

            foreach (var emailInvite in invitesToBeSent)
            {
                var user = await _userRepository.GetAsync(new UserId(emailInvite.UserId)).ConfigureAwait(false);

                if (user is null)
                    throw new NotFoundValidationException(emailInvite.UserId);

                // Send emails
                await _gridEmailSenderSender.SendEmailAsync(user, emailInvite).ConfigureAwait(false);

                // Update email event isSent = true
                emailInvite.IsSent = true;
                await _emailEventRepository.UpdateAsync(emailInvite).ConfigureAwait(false);

                // Log ?
            }

            return Unit.Value;
        }
    }
}
