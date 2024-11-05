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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using Energinet.DataHub.MarketParticipant.Application.Commands.BalanceResponsibility;
using Energinet.DataHub.MarketParticipant.Domain;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;
using NodaTime.Extensions;
using ValidationException = System.ComponentModel.DataAnnotations.ValidationException;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.BalanceResponsibility;

public sealed class ImportBalanceResponsibilitiesHandler : IRequestHandler<ImportBalanceResponsibilitiesCommand>
{
    private readonly IUnitOfWorkProvider _unitOfWorkProvider;
    private readonly IActorRepository _actorRepository;
    private readonly IBalanceResponsibilityRequestRepository _balanceResponsibilityRequestsRepository;

    public ImportBalanceResponsibilitiesHandler(
        IUnitOfWorkProvider unitOfWorkProvider,
        IActorRepository actorRepository,
        IBalanceResponsibilityRequestRepository balanceResponsibilityRequestsRepository)
    {
        _unitOfWorkProvider = unitOfWorkProvider;
        _actorRepository = actorRepository;
        _balanceResponsibilityRequestsRepository = balanceResponsibilityRequestsRepository;
    }

    public async Task Handle(ImportBalanceResponsibilitiesCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var unitOfWork = await _unitOfWorkProvider.NewUnitOfWorkAsync().ConfigureAwait(false);

        await using (unitOfWork.ConfigureAwait(false))
        {
            var actorIdLookup = (await _actorRepository.GetActorsAsync().ConfigureAwait(false))
                .Where(x => x.MarketRoles.Any(y => y.Function == EicFunction.BalanceResponsibleParty))
                .ToDictionary(x => x.ActorNumber.Value, x => x.Id);

            using var streamReader = new StreamReader(request.Stream);
            using var csvReader = new CsvReader(streamReader, new CsvConfiguration(new CultureInfo("en-US")));

            await foreach (var record in csvReader.GetRecordsAsync<BalanceResponsibilityRequestRecord>(cancellationToken).ConfigureAwait(false))
            {
                if (!actorIdLookup.TryGetValue(record.BalanceResponsible, out var balanceResponsibleId))
                {
                    throw new ValidationException($"Balance responsible with actor number '{record.BalanceResponsible}' was not found.");
                }

                var balanceResponsibilityRequest = new BalanceResponsibilityRequest(
                    ActorNumber.Create(record.EnergySupplier),
                    ActorNumber.Create(record.BalanceResponsible),
                    new GridAreaCode(record.GridArea),
                    record.MeteringPointType,
                    record.ValidFrom.ToInstant(),
                    record.ValidTo?.ToInstant());

                await _balanceResponsibilityRequestsRepository.EnqueueAsync(balanceResponsibilityRequest).ConfigureAwait(false);
                await _balanceResponsibilityRequestsRepository.ProcessNextRequestsAsync(balanceResponsibleId).ConfigureAwait(false);
            }

            await unitOfWork.CommitAsync().ConfigureAwait(false);
        }
    }

    // ReSharper disable once ClassNeverInstantiated.Local
    private sealed record BalanceResponsibilityRequestRecord(
        string EnergySupplier,
        string BalanceResponsible,
        string GridArea,
        MeteringPointType MeteringPointType,
        DateTimeOffset ValidFrom,
        DateTimeOffset? ValidTo);
}
