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
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Email
{
    public sealed class SendEmailHandler : IRequestHandler<SendEmailCommand, Unit>
    {
        private readonly IEmailEventRepository _emailEventRepository;
        private readonly IEmailSender _emailSender;

        public SendEmailHandler(
            IEmailEventRepository emailEventRepository,
            IEmailSender emailSender)
        {
            _emailEventRepository = emailEventRepository;
            _emailSender = emailSender;
        }

        public async Task<Unit> Handle(SendEmailCommand request, CancellationToken cancellationToken)
        {
            var emailEvents = await _emailEventRepository
                .GetAllPendingEmailEventsAsync()
                .ConfigureAwait(false);

            foreach (var emailEvent in emailEvents)
            {
                await _emailSender.SendEmailAsync(emailEvent.Email, emailEvent).ConfigureAwait(false);
                await _emailEventRepository.MarkAsSentAsync(emailEvent).ConfigureAwait(false);
            }

            return Unit.Value;
        }
    }
}
