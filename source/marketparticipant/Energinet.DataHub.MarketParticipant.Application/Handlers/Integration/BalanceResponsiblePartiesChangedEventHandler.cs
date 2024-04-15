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
using Energinet.DataHub.MarketParticipant.Domain;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Email;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using NodaTime.Serialization.Protobuf;
using MeteringPointType = Energinet.DataHub.MarketParticipant.Application.Contracts.MeteringPointType;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Integration;

#pragma warning disable CA1711
public sealed class BalanceResponsiblePartiesChangedEventHandler(
#pragma warning restore CA1711
    IBalanceResponsibilityRequestRepository balanceResponsibilityRequestRepository,
    IEmailEventRepository emailEventRepository,
    IUnitOfWorkProvider unitOfWorkProvider,
    EmailRecipientConfig emailRecipientConfig)
    : IBalanceResponsiblePartiesChangedEventHandler
{
    public async Task HandleAsync(Contracts.BalanceResponsiblePartiesChanged balanceResponsiblePartiesChanged)
    {
        ArgumentNullException.ThrowIfNull(balanceResponsiblePartiesChanged);

        var uow = await unitOfWorkProvider
            .NewUnitOfWorkAsync()
            .ConfigureAwait(false);

        await using (uow.ConfigureAwait(false))
        {
            var balanceResponsibilityRequest = Map(balanceResponsiblePartiesChanged);

            await balanceResponsibilityRequestRepository
                .EnqueueAsync(balanceResponsibilityRequest)
                .ConfigureAwait(false);

            await BuildAndSendEmailAsync(balanceResponsibilityRequest)
                .ConfigureAwait(false);

            await uow.CommitAsync().ConfigureAwait(false);
        }
    }

    private static BalanceResponsibilityRequest Map(Contracts.BalanceResponsiblePartiesChanged balanceResponsiblePartiesChanged)
    {
        var meteringPointType = balanceResponsiblePartiesChanged.MeteringPointType switch
        {
            MeteringPointType.Mgaexchange => Domain.Model.MeteringPointType.E20Exchange,
            MeteringPointType.Production => Domain.Model.MeteringPointType.E18Production,
            MeteringPointType.Consumption => Domain.Model.MeteringPointType.E17Consumption,
            _ => throw new InvalidOperationException("Unexpected MeteringPointType in BalanceResponsiblePartiesChanged event.")
        };

        return new BalanceResponsibilityRequest(
            ActorNumber.Create(balanceResponsiblePartiesChanged.EnergySupplierId),
            ActorNumber.Create(balanceResponsiblePartiesChanged.BalanceResponsibleId),
            new GridAreaCode(balanceResponsiblePartiesChanged.GridAreaCode),
            meteringPointType,
            balanceResponsiblePartiesChanged.ValidFrom.ToInstant(),
            balanceResponsiblePartiesChanged.ValidTo?.ToInstant());
    }

    private Task BuildAndSendEmailAsync(BalanceResponsibilityRequest balanceResponsibilityRequest)
    {
        return emailEventRepository.InsertAsync(
            new EmailEvent(
                new EmailAddress(emailRecipientConfig.BalanceResponsibleChangedNotificationToEmail),
                new BalanceResponsiblePartiesChangedEmailTemplate(balanceResponsibilityRequest)));
    }
}
