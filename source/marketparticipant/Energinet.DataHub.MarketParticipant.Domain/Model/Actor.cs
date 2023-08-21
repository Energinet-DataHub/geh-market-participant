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
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Energinet.DataHub.MarketParticipant.Domain.Model.Events;

namespace Energinet.DataHub.MarketParticipant.Domain.Model;

public sealed class Actor : IPublishDomainEvents
{
    private readonly List<DomainEvent> _domainEvents = new();
    private readonly List<ActorMarketRole> _marketRoles = new();
    private readonly ActorStatusTransitioner _actorStatusTransitioner;
    private ExternalActorId? _externalActorId;

    public Actor(OrganizationId organizationId, ActorNumber actorNumber)
    {
        Id = new ActorId(Guid.Empty);
        OrganizationId = organizationId;
        ActorNumber = actorNumber;
        Name = new ActorName(string.Empty);
        _actorStatusTransitioner = new ActorStatusTransitioner();
    }

    public Actor(
        ActorId id,
        OrganizationId organizationId,
        ExternalActorId? externalActorId,
        ActorNumber actorNumber,
        ActorStatus actorStatus,
        IEnumerable<ActorMarketRole> marketRoles,
        ActorName name)
    {
        Id = id;
        OrganizationId = organizationId;
        ActorNumber = actorNumber;
        Name = name;
        _externalActorId = externalActorId;
        _actorStatusTransitioner = new ActorStatusTransitioner(actorStatus);
        _marketRoles.AddRange(marketRoles);
    }

    /// <summary>
    /// The internal id of actor.
    /// </summary>
    public ActorId Id { get; }

    /// <summary>
    /// The id of the organization the actor belongs to.
    /// </summary>
    public OrganizationId OrganizationId { get; }

    /// <summary>
    /// The external actor id for integrating Azure AD and domains.
    /// </summary>
    public ExternalActorId? ExternalActorId
    {
        get => _externalActorId;
        set
        {
            if (value != null)
            {
                _domainEvents.Add(new ActorActivated(ActorNumber, value));
            }

            _externalActorId = value;
        }
    }

    /// <summary>
    /// The global location number of the current actor.
    /// </summary>
    public ActorNumber ActorNumber { get; }

    /// <summary>
    /// The status of the current actor.
    /// </summary>
    public ActorStatus Status
    {
        get => _actorStatusTransitioner.Status;
        set
        {
            if (value == ActorStatus.Active && value != _actorStatusTransitioner.Status)
            {
                Activate();
            }
            else
            {
                _actorStatusTransitioner.Status = value;
            }
        }
    }

    /// <summary>
    /// The Name of the current actor.
    /// </summary>
    public ActorName Name { get; set; }

    /// <summary>
    /// The roles (functions and permissions) assigned to the current actor.
    /// </summary>
    public IReadOnlyList<ActorMarketRole> MarketRoles => _marketRoles;

    IReadOnlyList<DomainEvent> IPublishDomainEvents.DomainEvents => _domainEvents;

    /// <summary>
    /// Adds a new role from the current actor.
    /// This is only allowed for 'New' actors.
    /// </summary>
    /// <param name="marketRole">The new market role to add.</param>
    public void AddMarketRole(ActorMarketRole marketRole)
    {
        ArgumentNullException.ThrowIfNull(marketRole);

        if (Status != ActorStatus.New)
        {
            throw new ValidationException("It is only allowed to modify market roles for actors marked as 'New'.");
        }

        if (_marketRoles.Any(role => role.Function == marketRole.Function))
        {
            throw new ValidationException("The market roles cannot contain duplicates.");
        }

        _marketRoles.Add(marketRole);
    }

    /// <summary>
    /// Removes an existing role from the current actor.
    /// This is only allowed for 'New' actors.
    /// </summary>
    /// <param name="marketRole">The existing market role to remove.</param>
    public void RemoveMarketRole(ActorMarketRole marketRole)
    {
        ArgumentNullException.ThrowIfNull(marketRole);

        if (Status != ActorStatus.New)
        {
            throw new ValidationException("It is only allowed to modify market roles for actors marked as 'New'.");
        }

        if (!_marketRoles.Remove(marketRole))
        {
            throw new ValidationException($"Market role for {marketRole.Function} was not found.");
        }
    }

    /// <summary>
    /// Activates the current actor, the status changes to Active.
    /// Only New actors can be activated.
    /// </summary>
    public void Activate()
    {
        _actorStatusTransitioner.Activate();

        foreach (var marketRole in _marketRoles.Where(role => role.Function == EicFunction.GridAccessProvider))
        {
            foreach (var gridArea in marketRole.GridAreas)
            {
                _domainEvents.Add(new GridAreaOwnershipAssigned(
                    ActorNumber,
                    marketRole.Function,
                    gridArea.Id));
            }
        }
    }

    /// <summary>
    /// Deactivates the current actor, the status changes to Inactive.
    /// Only New, Active and Passive actors can be deactivated.
    /// </summary>
    public void Deactivate() => _actorStatusTransitioner.Deactivate();

    /// <summary>
    /// Passive actors have certain domain-specific actions that can be performed.
    /// Only Active and New actors can be set to passive.
    /// </summary>
    public void SetAsPassive() => _actorStatusTransitioner.SetAsPassive();

    void IPublishDomainEvents.ClearPublishedDomainEvents()
    {
        _domainEvents.Clear();
    }

    Guid IPublishDomainEvents.GetAggregateIdForDomainEvents()
    {
        return Id.Value;
    }
}
