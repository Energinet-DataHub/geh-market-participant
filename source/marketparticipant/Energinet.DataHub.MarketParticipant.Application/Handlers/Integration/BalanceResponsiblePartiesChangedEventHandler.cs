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
using Energinet.DataHub.MarketParticipant.Domain.Model.Email;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using NodaTime.Serialization.Protobuf;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Integration;

#pragma warning disable CA1711
public sealed class BalanceResponsiblePartiesChangedEventHandler(
#pragma warning restore CA1711
    IEmailEventRepository emailEventRepository,
    EmailRecipientConfig emailRecipientConfig)
    : IBalanceResponsiblePartiesChangedEventHandler
{
    public Task HandleAsync(Contracts.BalanceResponsiblePartiesChanged balanceResponsiblePartiesChanged)
    {
        ArgumentNullException.ThrowIfNull(balanceResponsiblePartiesChanged);
        return BuildAndSendEmailAsync(Map(balanceResponsiblePartiesChanged));
    }

    private static BalanceResponsiblePartiesChanged Map(Contracts.BalanceResponsiblePartiesChanged balanceResponsiblePartiesChanged)
    {
        return new BalanceResponsiblePartiesChanged(
            ActorNumber.Create(balanceResponsiblePartiesChanged.EnergySupplierId),
            ActorNumber.Create(balanceResponsiblePartiesChanged.BalanceResponsibleId),
            new GridAreaCode(balanceResponsiblePartiesChanged.GridAreaCode),
            balanceResponsiblePartiesChanged.Received.ToInstant(),
            balanceResponsiblePartiesChanged.ValidFrom.ToInstant(),
            balanceResponsiblePartiesChanged.ValidTo?.ToInstant());
    }

    private Task BuildAndSendEmailAsync(BalanceResponsiblePartiesChanged balanceResponsiblePartiesChanged)
    {
        return emailEventRepository.InsertAsync(
            new EmailEvent(
                new EmailAddress(emailRecipientConfig.BalanceResponsibleChangedNotificationToEmail),
                new BalanceResponsiblePartiesChangedEmailTemplate(balanceResponsiblePartiesChanged)));
    }
}
