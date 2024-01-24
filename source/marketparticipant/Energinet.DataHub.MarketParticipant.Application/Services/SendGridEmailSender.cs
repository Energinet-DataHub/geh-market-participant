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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Errors.Model;
using SendGrid.Helpers.Mail;
using EmailAddress = Energinet.DataHub.MarketParticipant.Domain.Model.EmailAddress;

namespace Energinet.DataHub.MarketParticipant.Application.Services
{
    public class SendGridEmailSender : IEmailSender
    {
        private readonly InviteConfig _config;
        private readonly ILogger<SendGridEmailSender> _logger;
        private readonly ISendGridClient _client;

        public SendGridEmailSender(
            InviteConfig config,
            ISendGridClient sendGridClient,
            ILogger<SendGridEmailSender> logger)
        {
            _config = config;
            _logger = logger;
            _client = sendGridClient;
        }

        public Task<bool> SendEmailAsync(EmailAddress emailAddress, EmailEvent emailEvent)
        {
            ArgumentNullException.ThrowIfNull(emailAddress);
            ArgumentNullException.ThrowIfNull(emailEvent);

            return emailEvent.EmailEventType switch
            {
                EmailEventType.UserInvite => SendUserInviteAsync(emailAddress),
                EmailEventType.UserAssignedToActor => SendUserAssignedToActorAsync(emailAddress),
                _ => throw new NotFoundException("EmailEventType not recognized"),
            };
        }

        private async Task<bool> SendUserInviteAsync(EmailAddress userEmailAddress)
        {
            var inviteUrl = _config.UserInviteFlow + "&nonce=defaultNonce&scope=openid&response_type=code&prompt=login&code_challenge_method=S256&code_challenge=defaultCodeChallenge";
            var from = new SendGrid.Helpers.Mail.EmailAddress(_config.UserInviteFromEmail);
            var to = new SendGrid.Helpers.Mail.EmailAddress(userEmailAddress.Address);
            var subject = $"Invitation til DataHub {_config.EnvironmentDescription}";
            var htmlContent = $"Invitation til DataHub {_config.EnvironmentDescription}<br /><br />Bliv oprettet <a href=\"{inviteUrl}\">her</a>" +
                              $"<br /><br />Brugeroprettelsen skal færdiggøres indenfor 24 timer.";
            return await SendAsync(userEmailAddress, from, to, subject, htmlContent).ConfigureAwait(false);
        }

        private async Task<bool> SendUserAssignedToActorAsync(EmailAddress userEmailAddress)
        {
            return await SendAsync(
                userEmailAddress,
                from: new SendGrid.Helpers.Mail.EmailAddress(_config.UserInviteFromEmail),
                to: new SendGrid.Helpers.Mail.EmailAddress(userEmailAddress.Address),
                subject: $"Inviteret til ny aktør i DataHub {_config.EnvironmentDescription}",
                htmlContent: $"Du er blevet inviteret til en ny aktør. Du kan tilgå den nye aktør i DataHub {_config.EnvironmentDescription}.").ConfigureAwait(false);
        }

        private async Task<bool> SendAsync(EmailAddress userEmailAddress, SendGrid.Helpers.Mail.EmailAddress from, SendGrid.Helpers.Mail.EmailAddress to, string subject, string htmlContent)
        {
            var msg = MailHelper.CreateSingleEmail(from, to, subject, string.Empty, htmlContent);
            msg.AddBcc(new SendGrid.Helpers.Mail.EmailAddress(_config.UserInviteBccEmail));

            var response = await _client.SendEmailAsync(msg).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("User invite email sent successfully to {Address}.", userEmailAddress.Address);
            }
            else
            {
                throw new NotSupportedException("User invite email return error response code:  " + response.StatusCode);
            }

            return response.IsSuccessStatusCode;
        }
    }
}
