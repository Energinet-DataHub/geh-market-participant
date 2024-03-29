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

namespace Energinet.DataHub.MarketParticipant.Domain.Model.Email;

public sealed class EmailEvent
{
    public EmailEvent(
        int id,
        EmailAddress emailAddress,
        DateTimeOffset created,
        DateTimeOffset? sent,
        EmailTemplate emailTemplate)
    {
        Id = id;
        Email = emailAddress;
        Created = created;
        Sent = sent;
        EmailTemplate = emailTemplate;
    }

    public EmailEvent(EmailAddress emailAddress, EmailTemplate emailTemplate)
    {
        Email = emailAddress;
        EmailTemplate = emailTemplate;
    }

    public int Id { get; }
    public EmailAddress Email { get; }
    public DateTimeOffset Created { get; }
    public DateTimeOffset? Sent { get; set; }
    public EmailTemplate EmailTemplate { get; }
}
