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
using Energinet.DataHub.MarketParticipant.Domain.Model.Events;

namespace Energinet.DataHub.MarketParticipant.Domain.Model.Email;

public sealed class BalanceResponsiblePartiesChangedEmailTemplate : EmailTemplate
{
    public BalanceResponsiblePartiesChangedEmailTemplate(BalanceResponsiblePartiesChanged balanceResponsiblePartiesChanged)
        : base(EmailTemplateId.BalanceResponsiblePartiesChanged, PrepareParameters(balanceResponsiblePartiesChanged))
    {
    }

    public BalanceResponsiblePartiesChangedEmailTemplate(IReadOnlyDictionary<string, string> parameters)
        : base(EmailTemplateId.BalanceResponsiblePartiesChanged, parameters)
    {
    }

    private static Dictionary<string, string> PrepareParameters(BalanceResponsiblePartiesChanged balanceResponsiblePartiesChanged)
    {
        ArgumentNullException.ThrowIfNull(balanceResponsiblePartiesChanged);

        return new Dictionary<string, string>
        {
            { "actor_balance_responsible", balanceResponsiblePartiesChanged.BalanceResponsibleParty.Value },
            { "actor_supplier", balanceResponsiblePartiesChanged.ElectricalSupplier.Value },
            { "grid_area_code", balanceResponsiblePartiesChanged.GridAreaCode.Value },
            { "valid_from", balanceResponsiblePartiesChanged.ValidFrom.ToDateTimeUtc().ToLongDateString() },
            { "valid_to", balanceResponsiblePartiesChanged.ValidTo?.ToDateTimeUtc().ToLongDateString() ?? string.Empty },
        };
    }
}
