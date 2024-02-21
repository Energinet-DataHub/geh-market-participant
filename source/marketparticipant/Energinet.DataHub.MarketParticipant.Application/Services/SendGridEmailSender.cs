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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Services.Email;
using Energinet.DataHub.MarketParticipant.Domain.Model.Email;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;
using EmailAddress = Energinet.DataHub.MarketParticipant.Domain.Model.EmailAddress;

namespace Energinet.DataHub.MarketParticipant.Application.Services
{
    public sealed class SendGridEmailSender : IEmailSender
    {
        private readonly EmailInviteRecipientConfig _config;
        private readonly ILogger<SendGridEmailSender> _logger;
        private readonly ISendGridClient _client;
        private readonly IEmailContentGenerator _emailContentGenerator;

        public SendGridEmailSender(
            EmailInviteRecipientConfig config,
            ISendGridClient sendGridClient,
            IEmailContentGenerator emailContentGenerator,
            ILogger<SendGridEmailSender> logger)
        {
            _config = config;
            _logger = logger;
            _client = sendGridClient;
            _emailContentGenerator = emailContentGenerator;
        }

        public async Task<bool> SendEmailAsync(EmailAddress emailAddress, EmailEvent emailEvent)
        {
            ArgumentNullException.ThrowIfNull(emailAddress);
            ArgumentNullException.ThrowIfNull(emailEvent);

            var generatedEmail = await _emailContentGenerator
                .GenerateAsync(emailEvent.EmailTemplate, GatherTemplateParameters())
                .ConfigureAwait(false);

            return await SendAsync(
                new SendGrid.Helpers.Mail.EmailAddress(_config.SenderEmail),
                new SendGrid.Helpers.Mail.EmailAddress(emailAddress.Address),
                generatedEmail.Subject,
                generatedEmail.HtmlContent)
                .ConfigureAwait(false);
        }

        private IReadOnlyDictionary<string, string> GatherTemplateParameters()
        {
            var environmentShort = string.Empty;
            var environmentLong = string.Empty;

            if (_config.EnvironmentDescription != null)
            {
                environmentShort = $"({_config.EnvironmentDescription})";
                environmentLong = $"(Miljø: {_config.EnvironmentDescription})";
            }

            return new Dictionary<string, string>
            {
                { "environment_short", environmentShort },
                { "environment_long", environmentLong },
                { "invite_link", _config.UserInviteFlow + "&nonce=defaultNonce&scope=openid&response_type=code&prompt=login&code_challenge_method=S256&code_challenge=defaultCodeChallenge" },
            };
        }

        private async Task<bool> SendAsync(
            SendGrid.Helpers.Mail.EmailAddress from,
            SendGrid.Helpers.Mail.EmailAddress to,
            string subject,
            string htmlContent)
        {
            var msg = MailHelper.CreateSingleEmail(from, to, subject, string.Empty, htmlContent);
            msg.AddBcc(new SendGrid.Helpers.Mail.EmailAddress(_config.BccEmail));

            var response = await _client.SendEmailAsync(msg).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Email sent successfully to {Address}.", to.Email);
            }
            else
            {
                throw new NotSupportedException("Email failed with error code: " + response.StatusCode);
            }

            return response.IsSuccessStatusCode;
        }
    }
}
