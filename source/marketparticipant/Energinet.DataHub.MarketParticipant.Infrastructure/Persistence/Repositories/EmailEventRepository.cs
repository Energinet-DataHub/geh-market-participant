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
using System.Text.Json;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Email;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Microsoft.EntityFrameworkCore;
using SendGrid.Helpers.Errors.Model;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories
{
    public sealed class EmailEventRepository : IEmailEventRepository
    {
        private readonly IMarketParticipantDbContext _context;

        public EmailEventRepository(IMarketParticipantDbContext context)
        {
            _context = context;
        }

        public Task InsertAsync(EmailEvent emailEvent)
        {
            ArgumentNullException.ThrowIfNull(emailEvent);

            var emailEventEntity = new EmailEventEntity
            {
                Email = emailEvent.Email.Address,
                Created = DateTimeOffset.UtcNow,
                TemplateId = (int)emailEvent.EmailTemplate.TemplateId,
                TemplateParameters = JsonSerializer.Serialize(emailEvent.EmailTemplate.TemplateParameters),
            };

            _context.EmailEventEntries.Add(emailEventEntity);

            return _context.SaveChangesAsync();
        }

        public Task MarkAsSentAsync(EmailEvent emailEvent)
        {
            ArgumentNullException.ThrowIfNull(emailEvent);

            var emailEventToUpdate = _context.EmailEventEntries.FirstOrDefault(e => e.Id == emailEvent.Id);

            if (emailEventToUpdate != null)
            {
                emailEventToUpdate.Sent = DateTimeOffset.UtcNow;
                return _context.SaveChangesAsync();
            }

            throw new NotFoundException($"Email event with id {emailEvent.Id} was not found");
        }

        public async Task<IEnumerable<EmailEvent>> GetAllPendingEmailEventsAsync()
        {
            var emailToBeSent = await _context.EmailEventEntries
                .Where(e => e.Sent == null)
                .ToListAsync()
                .ConfigureAwait(false);

            return emailToBeSent.Select(MapTo);
        }

        private static EmailEvent MapTo(EmailEventEntity emailEventEntity)
        {
            var templateId = (EmailTemplateId)emailEventEntity.TemplateId;
            var templateParameters = JsonSerializer.Deserialize<Dictionary<string, string>>(emailEventEntity.TemplateParameters);
            if (templateParameters == null)
                throw new InvalidOperationException($"Template parameters for event {emailEventEntity.Id} are invalid.");

            EmailTemplate mailTemplate;

            switch (templateId)
            {
                case EmailTemplateId.UserInvite:
                    mailTemplate = new UserInviteEmailTemplate(templateId, templateParameters);
                    break;
                case EmailTemplateId.UserAssignedToActor:
                    mailTemplate = new UserInviteEmailTemplate(templateId, templateParameters);
                    break;
                default:
                    throw new InvalidOperationException($"Template id for event {emailEventEntity.Id} is invalid.");
            }

            return new EmailEvent(
                emailEventEntity.Id,
                new EmailAddress(emailEventEntity.Email),
                emailEventEntity.Created,
                emailEventEntity.Sent,
                mailTemplate);
        }
    }
}
