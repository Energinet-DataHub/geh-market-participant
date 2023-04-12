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

using Energinet.DataHub.MarketParticipant.Application;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Microsoft.Extensions.Logging;
using SendGrid;
using SimpleInjector;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.Organization.Email;

internal static class EmailSenderRegistration
{
    public static void AddSendGridEmailSenderClient(this Container container)
    {
        container.Register<IEmailSender>(
            () =>
            {
                var configuration = container.GetInstance<InviteConfig>();
                var senGridClient = container.GetInstance<ISendGridClient>();
                var logger = container.GetInstance<ILogger<SendGridEmailSender>>();

                return new SendGridEmailSender(
                    configuration,
                    senGridClient,
                    logger);
            },
            Lifestyle.Scoped);
    }
}
