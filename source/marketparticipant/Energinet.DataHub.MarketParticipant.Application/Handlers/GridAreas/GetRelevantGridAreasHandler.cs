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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.Core.App.Common.Abstractions.Users;
using Energinet.DataHub.MarketParticipant.Application.Commands.GridAreas;
using Energinet.DataHub.MarketParticipant.Application.Security;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;
using NodaTime;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.GridAreas;

public sealed class GetRelevantGridAreasHandler : IRequestHandler<GetRelevantGridAreasCommand, GetGridAreasResponse>
{
    private readonly IActorRepository _actorRepository;
    private readonly IGridAreaRepository _gridAreaRepository;
    private readonly IUserContext<FrontendUser> _userContext;

    public GetRelevantGridAreasHandler(IActorRepository actorRepository, IGridAreaRepository gridAreaRepository, IUserContext<FrontendUser> userContext)
    {
        _actorRepository = actorRepository;
        _gridAreaRepository = gridAreaRepository;
        _userContext = userContext;
    }

    public async Task<GetGridAreasResponse> Handle(GetRelevantGridAreasCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var startDate = request.Period.Start.ToDateTimeOffset();
        var endDate = request.Period.End.ToDateTimeOffset();

        var gridAreas = await _gridAreaRepository.GetAsync().ConfigureAwait(false);
        var filteredByDateGridAreas = gridAreas.Where(ga => IsRelevantGridArea(ga, startDate, endDate));

        var actor = await _actorRepository.GetAsync(new ActorId(_userContext.CurrentUser.ActorId)).ConfigureAwait(false);
        NotFoundValidationException.ThrowIfNull(actor, _userContext.CurrentUser.ActorId);
        var actorGridAreaIds = actor.MarketRole.GridAreas.Select(ga => ga.Id);
        var relevantGridAreas = filteredByDateGridAreas.Where(ga => actorGridAreaIds.Contains(ga.Id));

        return new GetGridAreasResponse(relevantGridAreas.Select(gridArea => new GridAreaDto(
            gridArea.Id.Value,
            gridArea.Code.Value,
            gridArea.Name.Value,
            gridArea.PriceAreaCode.ToString(),
            gridArea.Type,
            gridArea.ValidFrom,
            gridArea.ValidTo)));
    }

    private static bool IsRelevantGridArea(GridArea gridArea, DateTimeOffset startDate, DateTimeOffset endDate)
    {
        if (!gridArea.ValidTo.HasValue)
        {
            return gridArea.ValidFrom <= endDate;
        }

        // formula from https://www.baeldung.com/java-check-two-date-ranges-overlap
        var overlap = Math.Min(gridArea.ValidTo.Value.Ticks, endDate.Ticks) - Math.Max(gridArea.ValidFrom.Ticks, startDate.Ticks);
        return overlap >= 0;
    }
}
