﻿// Copyright 2020 Energinet DataHub A/S
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
using System.ComponentModel;
using System.Text.Json.Serialization;
using NodaTime;

namespace Energinet.DataHub.MarketParticipant.Domain.Model.Events;

public sealed class ActorCertificateCredentialsAssigned : DomainEvent
{
    [JsonConstructor]
    [Browsable(false)]
    public ActorCertificateCredentialsAssigned(
        Guid eventId,
        ActorNumber actorNumber,
        EicFunction actorRole,
        string certificateThumbprint,
        Instant validFrom)
    {
        EventId = eventId;
        ActorNumber = actorNumber;
        ActorRole = actorRole;
        CertificateThumbprint = certificateThumbprint;
        ValidFrom = validFrom;
    }

    public ActorCertificateCredentialsAssigned(ActorNumber actorNumber, EicFunction actorRole, string certificateThumbprint)
    {
        EventId = Guid.NewGuid();
        ActorNumber = actorNumber;
        ActorRole = actorRole;
        CertificateThumbprint = certificateThumbprint;
        ValidFrom = Clock.Instance.GetCurrentInstant();
    }

    public ActorNumber ActorNumber { get; }
    public EicFunction ActorRole { get; }
    public string CertificateThumbprint { get; }
    public Instant ValidFrom { get; }
}
