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
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Options;
using Energinet.DataHub.MarketParticipant.Domain;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Email;
using Energinet.DataHub.MarketParticipant.Domain.Model.Events;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Microsoft.Extensions.Options;
using NodaTime.Serialization.Protobuf;
using MeteringPointType = Energinet.DataHub.MarketParticipant.Application.Contracts.MeteringPointType;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Integration;

#pragma warning disable CA1711
public sealed class BalanceResponsiblePartiesChangedEventHandler : IBalanceResponsiblePartiesChangedEventHandler
{
#pragma warning disable CA1711

    private readonly IActorRepository _actorRepository;
    private readonly IDomainEventRepository _domainEventRepository;
    private readonly IBalanceResponsibilityRequestRepository _balanceResponsibilityRequestRepository;
    private readonly IEmailEventRepository _emailEventRepository;
    private readonly IUnitOfWorkProvider _unitOfWorkProvider;
    private readonly IOptions<BalanceResponsibleChangedOptions> _balanceResponsibleChangedOptions;

    public BalanceResponsiblePartiesChangedEventHandler(
        IActorRepository actorRepository,
        IDomainEventRepository domainEventRepository,
        IBalanceResponsibilityRequestRepository balanceResponsibilityRequestRepository,
        IEmailEventRepository emailEventRepository,
        IUnitOfWorkProvider unitOfWorkProvider,
        IOptions<BalanceResponsibleChangedOptions> balanceResponsibleChangedOptions)
    {
        _actorRepository = actorRepository;
        _domainEventRepository = domainEventRepository;
        _balanceResponsibilityRequestRepository = balanceResponsibilityRequestRepository;
        _emailEventRepository = emailEventRepository;
        _unitOfWorkProvider = unitOfWorkProvider;
        _balanceResponsibleChangedOptions = balanceResponsibleChangedOptions;
    }

    public async Task HandleAsync(Contracts.BalanceResponsiblePartiesChanged balanceResponsiblePartiesChanged)
    {
        ArgumentNullException.ThrowIfNull(balanceResponsiblePartiesChanged);

        var allActors = await _actorRepository
                .GetActorsAsync()
                .ConfigureAwait(false);

        var notificationTargets = allActors
            .Where(actor =>
                actor.Status == ActorStatus.Active &&
                actor.MarketRoles.Any(mr => mr.Function == EicFunction.DataHubAdministrator))
            .Select(actor => actor.Id);

        var uow = await _unitOfWorkProvider
            .NewUnitOfWorkAsync()
            .ConfigureAwait(false);

        await using (uow.ConfigureAwait(false))
        {
            var balanceResponsibilityRequest = Map(balanceResponsiblePartiesChanged);

            await _balanceResponsibilityRequestRepository
                .EnqueueAsync(balanceResponsibilityRequest)
                .ConfigureAwait(false);

            foreach (var target in notificationTargets)
            {
                await _domainEventRepository
                    .EnqueueAsync(new NewBalanceResponsibilityReceived(target, balanceResponsibilityRequest.EnergySupplier))
                    .ConfigureAwait(false);
            }

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
        return _emailEventRepository.InsertAsync(
            new EmailEvent(
                new EmailAddress(_balanceResponsibleChangedOptions.Value.NotificationToEmail),
                new BalanceResponsiblePartiesChangedEmailTemplate(balanceResponsibilityRequest)));
    }
}
