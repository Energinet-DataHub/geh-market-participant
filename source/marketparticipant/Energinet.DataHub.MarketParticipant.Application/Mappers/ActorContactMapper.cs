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
using Energinet.DataHub.MarketParticipant.Application.Commands.Contacts;
using Energinet.DataHub.MarketParticipant.Domain.Model;

namespace Energinet.DataHub.MarketParticipant.Application.Mappers;

public static class ActorContactMapper
{
    public static ActorContactDto Map(ActorContact contact)
    {
        ArgumentNullException.ThrowIfNull(contact, nameof(contact));
        return new ActorContactDto(
            contact.Id.Value,
            contact.ActorId.Value,
            contact.Category,
            contact.Name,
            contact.Email.Address,
            contact.Phone?.Number);
    }
}
