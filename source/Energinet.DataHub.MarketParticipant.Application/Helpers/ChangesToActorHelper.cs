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
using Energinet.DataHub.MarketParticipant.Application.Commands.Actor;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.IntegrationEvents;

namespace Energinet.DataHub.MarketParticipant.Application.Helpers;

public sealed class ChangesToActorHelper : IChangesToActorHelper
{
    private readonly List<IIntegrationEvent> _changeEvents = new();

    public IEnumerable<IIntegrationEvent> FindChangesMadeToActor(Actor existingActor, UpdateActorCommand incomingActor)
    {
        ArgumentNullException.ThrowIfNull(existingActor, nameof(existingActor));
        ArgumentNullException.ThrowIfNull(incomingActor, nameof(incomingActor));

        AddChangeEventIfActorStatusChanged(existingActor, incomingActor.ChangeActor.Status);
        AddChangeEventsIfMarketRolesOrChildrenChanged(existingActor, incomingActor.ChangeActor.MarketRoles);

        return _changeEvents;
    }

    private void AddChangeEventIfActorStatusChanged(Actor existingActor, string incomingStatus)
    {
        var newStatus = Enum.Parse<ActorStatus>(incomingStatus);
        if (existingActor.Status != newStatus)
        {
            _changeEvents.Add(new ActorStatusChangedIntegrationEvent
            {
                ActorId = existingActor.Id,
                Status = newStatus
            });
        }
    }

    private void AddChangeEventsIfMarketRolesOrChildrenChanged(
        Actor existingActor,
        IEnumerable<ActorMarketRoleDto> incomingMarketRoles)
    {
        var existingEicFunctions = existingActor
            .MarketRoles
            .Select(marketRole => marketRole.Function)
            .ToList();

        var incomingEicFunctions = incomingMarketRoles
            .Select(marketRole => Enum.Parse<EicFunction>(marketRole.EicFunction))
            .ToList();

        var eicFunctionsToRemoveFromActor = existingEicFunctions.Except(incomingEicFunctions);

        var eicFunctionsToAddToActor = incomingEicFunctions.Except(existingEicFunctions);

        // Loop over each market role that did not change to see if children of the market role have changed
        foreach (var existingEicFunction in existingEicFunctions)
        {
            if (incomingEicFunctions.Contains(existingEicFunction))
            {
                var existingGridAreas = existingActor
                    .MarketRoles
                    .First(marketRole => marketRole.Function == existingEicFunction)
                    .GridAreas;

                var incomingGridAreas = incomingMarketRoles
                    .First(marketRole => Enum.Parse<EicFunction>(marketRole.EicFunction) == existingEicFunction)
                    .GridAreas;

                var marketRole = new ActorMarketRole(existingActor.Id, existingEicFunction, existingGridAreas);

                AddChangeEventsIfGridAreasChanged(existingActor.Id, marketRole, incomingGridAreas);
            }
        }

        foreach (var eicFunction in eicFunctionsToRemoveFromActor)
        {
            var gridAreas = existingActor.MarketRoles.First(marketRole => marketRole.Function == eicFunction).GridAreas;
            var marketRole = new ActorMarketRole(existingActor.Id, eicFunction, gridAreas);

            _changeEvents.Add(new RemoveMarketRoleIntegrationEvent
            {
                ActorId = existingActor.Id,
                MarketRole = marketRole
            });

            AddChangeEventsForRemovedGridAreas(existingActor.Id, eicFunction, gridAreas);
        }

        foreach (var eicFunction in eicFunctionsToAddToActor)
        {
            var gridAreas = incomingMarketRoles.First(marketRole => Enum.Parse<EicFunction>(marketRole.EicFunction) == eicFunction).GridAreas;
            var marketRole = new ActorMarketRole(existingActor.Id, eicFunction, gridAreas.Select(gridArea => new ActorGridArea(gridArea.Id, gridArea.MeteringPointTypes.Select(meteringPointType => MeteringPointType.FromName(meteringPointType)))));

            _changeEvents.Add(new AddMarketRoleIntegrationEvent
            {
                ActorId = existingActor.Id,
                MarketRole = marketRole
            });

            AddChangeEventsForAddedGridAreas(existingActor.Id, eicFunction, gridAreas.Select(gridArea => new ActorGridArea(gridArea.Id, gridArea.MeteringPointTypes.Select(meteringPointType => MeteringPointType.FromName(meteringPointType)))));
        }
    }

    private void AddChangeEventsIfGridAreasChanged(Guid existingActorId, ActorMarketRole existingMarketRole, IEnumerable<ActorGridAreaDto> incomingGridAreas)
    {
        var incomingActorGridAreaDtos = incomingGridAreas.ToList();
        var incomingActorGridAreas = incomingActorGridAreaDtos.Select(gridArea => gridArea.Id).ToList();

        var gridAreaIdsToAddToActor = incomingActorGridAreas.Except(existingMarketRole.GridAreas.Select(gridArea => gridArea.Id));
        var gridAreasToAddToActor = gridAreaIdsToAddToActor
            .Select(id =>
                new ActorGridArea(
                    id,
                    incomingActorGridAreaDtos.First(x => x.Id == id).MeteringPointTypes.Select(x => MeteringPointType.FromName(x, false))));

        var gridAreaIdsToRemoveFromActor = existingMarketRole
            .GridAreas
            .Select(gridArea => gridArea.Id)
            .Except(incomingActorGridAreas);
        var gridAreasToRemoveFromActor = gridAreaIdsToRemoveFromActor
            .Select(id =>
                new ActorGridArea(
                    id,
                    incomingActorGridAreaDtos.First(x => x.Id == id).MeteringPointTypes.Select(x => MeteringPointType.FromName(x, false))));

        AddChangeEventsForAddedGridAreas(existingActorId, existingMarketRole.Function, gridAreasToAddToActor);
        AddChangeEventsForRemovedGridAreas(existingActorId, existingMarketRole.Function, gridAreasToRemoveFromActor);

        foreach (var gridArea in existingMarketRole.GridAreas)
        {
            if (incomingActorGridAreas.Contains(gridArea.Id))
            {
                AddChangeEventsIfMeteringPointTypeChanged(
                    existingActorId,
                    existingMarketRole.Function,
                    gridArea,
                    incomingActorGridAreaDtos.SelectMany(incomingGridArea => incomingGridArea.MeteringPointTypes.Select(m => MeteringPointType.FromName(m, false))));
            }
        }
    }

    private void AddChangeEventsIfMeteringPointTypeChanged(
        Guid existingActorId,
        EicFunction function,
        ActorGridArea existingGridArea,
        IEnumerable<MeteringPointType> incomingMeteringPointTypes)
    {
        var meteringPointTypes = incomingMeteringPointTypes.ToList();
        var meteringPointTypesToAddToActor = meteringPointTypes.Except(existingGridArea.MeteringPointTypes);
        var meteringPointTypesToRemoveFromActor = existingGridArea.MeteringPointTypes.Except(meteringPointTypes);

        AddChangeEventsForAddedMeteringPointTypes(
            existingActorId,
            function,
            existingGridArea.Id,
            meteringPointTypesToAddToActor);

        AddChangeEventsForRemovedMeteringPointTypes(
            existingActorId,
            function,
            existingGridArea.Id,
            meteringPointTypesToRemoveFromActor);
    }

    private void AddChangeEventsForAddedGridAreas(Guid existingActorId, EicFunction function, IEnumerable<ActorGridArea> gridAreas)
    {
        foreach (var gridArea in gridAreas)
        {
            _changeEvents.Add(new AddGridAreaIntegrationEvent
                {
                    ActorId = existingActorId,
                    Function = function,
                    GridArea = gridArea
                });

            AddChangeEventsForAddedMeteringPointTypes(
                existingActorId,
                function,
                gridArea.Id,
                gridArea.MeteringPointTypes);
        }
    }

    private void AddChangeEventsForAddedMeteringPointTypes(
        Guid existingActorId,
        EicFunction function,
        Guid gridAreaId,
        IEnumerable<MeteringPointType> gridAreaMeteringPointTypes)
    {
        foreach (var meteringPointType in gridAreaMeteringPointTypes)
        {
            _changeEvents.Add(new AddMeteringPointTypeIntegrationEvent
            {
                ActorId = existingActorId,
                Function = function,
                GridAreaId = gridAreaId,
                Type = meteringPointType
            });
        }
    }

    private void AddChangeEventsForRemovedGridAreas(Guid existingActorId, EicFunction function, IEnumerable<ActorGridArea> gridAreas)
    {
        foreach (var gridArea in gridAreas)
        {
            _changeEvents.Add(new RemoveGridAreaIntegrationEvent
            {
                ActorId = existingActorId,
                Function = function,
                GridArea = gridArea
            });

            AddChangeEventsForRemovedMeteringPointTypes(existingActorId, function, gridArea.Id, gridArea.MeteringPointTypes);
        }
    }

    private void AddChangeEventsForRemovedMeteringPointTypes(
        Guid existingActorId,
        EicFunction function,
        Guid gridAreaId,
        IEnumerable<MeteringPointType> gridAreaMeteringPointTypes)
    {
        foreach (var meteringPointType in gridAreaMeteringPointTypes)
        {
            _changeEvents.Add(new RemoveMeteringPointTypeIntegrationEvent
            {
                ActorId = existingActorId,
                Function = function,
                GridAreaId = gridAreaId,
                Type = meteringPointType
            });
        }
    }
}
