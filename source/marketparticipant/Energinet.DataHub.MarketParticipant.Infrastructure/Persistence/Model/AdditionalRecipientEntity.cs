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
using System.Collections.ObjectModel;
using System.Linq;
using Energinet.DataHub.MarketParticipant.Domain.Model;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;

public sealed class AdditionalRecipientEntity
{
    public int Id { get; set; }
    public Guid ActorId { get; set; }

    public Collection<AdditionalRecipientOfMeteringPointEntity> MeteringPoints { get; } = new();

    public AdditionalRecipient ToDomainModel()
    {
        return new AdditionalRecipient(
            new AdditionalRecipientId(Id),
            new ActorId(ActorId),
            MeteringPoints.Select(mp => mp.ToDomainModel()));
    }

    public void PatchFromDomainModel(AdditionalRecipient additionalRecipient)
    {
        ArgumentNullException.ThrowIfNull(additionalRecipient);
        ActorId = additionalRecipient.Actor.Value;

        var existingMeteringPoints = new HashSet<string>();
        var incomingMeteringPoints = additionalRecipient.OfMeteringPoints;

        for (var i = 0; i < MeteringPoints.Count; i++)
        {
            var existingMeteringPoint = MeteringPoints[i];
            existingMeteringPoints.Add(existingMeteringPoint.MeteringPointIdentification);

            if (incomingMeteringPoints.Contains(new MeteringPointIdentification(existingMeteringPoint.MeteringPointIdentification)))
                continue;

            MeteringPoints.RemoveAt(i--);
        }

        foreach (var incomingMeteringPoint in incomingMeteringPoints)
        {
            if (existingMeteringPoints.Contains(incomingMeteringPoint.Value))
                continue;

            var newMeteringPoint = new AdditionalRecipientOfMeteringPointEntity();
            newMeteringPoint.PatchFromDomainModel(incomingMeteringPoint);
            MeteringPoints.Add(newMeteringPoint);
        }
    }
}
