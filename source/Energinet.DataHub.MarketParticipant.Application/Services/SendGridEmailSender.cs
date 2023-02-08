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
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Errors.Model;
using SendGrid.Helpers.Mail;
using EmailAddress = Energinet.DataHub.MarketParticipant.Domain.Model.EmailAddress;

namespace Energinet.DataHub.MarketParticipant.Application.Services
{
    public class SendGridEmailSender : IEmailSender
    {
        private readonly ILogger<SendGridEmailSender> _logger;
        private ISendGridClient _client;

        public SendGridEmailSender(
            ISendGridClient sendGridClient,
            ILogger<SendGridEmailSender> logger)
        {
            _logger = logger;
            _client = sendGridClient;
        }

        public Task<bool> SendEmailAsync(User user, EmailEvent emailEvent)
        {
            ArgumentNullException.ThrowIfNull(user, nameof(user));

            return emailEvent?.EmailEventType switch
            {
                EmailEventType.UserInvite => SendUserInviteAsync(user.Email),
                _ => throw new NotFoundException("EmailEventType not recognized")
            };
        }

        private async Task<bool> SendUserInviteAsync(EmailAddress userEmailAddress)
        {
            var from = new SendGrid.Helpers.Mail.EmailAddress("info@test.dk");
            const string subject = "Invitation til DataHub";
            var to = new SendGrid.Helpers.Mail.EmailAddress(userEmailAddress.Address);
            var htmlContent = "Invitation til DatHub<br /><br />Bliv oprettet her: https://www.energinet.dk";
            var msg = MailHelper.CreateSingleEmail(from, to, subject, string.Empty, htmlContent);
            var response = await _client.SendEmailAsync(msg).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation($"User invite email sent successfully to {userEmailAddress.Address}");
            }
            else
            {
                _logger.LogError("User invite email return error response code:  " + response.StatusCode);
            }

            return response.IsSuccessStatusCode;
        }
    }
}
