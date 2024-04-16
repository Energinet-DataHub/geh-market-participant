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

namespace Energinet.DataHub.MarketParticipant.Domain.Model.Email;

public sealed class BalanceResponsiblePartiesChangedEmailTemplate : EmailTemplate
{
    public BalanceResponsiblePartiesChangedEmailTemplate(BalanceResponsibilityRequest balanceResponsibilityRequest)
        : base(EmailTemplateId.BalanceResponsiblePartiesChanged, PrepareParameters(balanceResponsibilityRequest))
    {
    }

    public BalanceResponsiblePartiesChangedEmailTemplate(IReadOnlyDictionary<string, string> parameters)
        : base(EmailTemplateId.BalanceResponsiblePartiesChanged, parameters)
    {
    }

    private static Dictionary<string, string> PrepareParameters(BalanceResponsibilityRequest balanceResponsibilityRequest)
    {
        ArgumentNullException.ThrowIfNull(balanceResponsibilityRequest);

        return new Dictionary<string, string>
        {
            { "actor_balance_responsible", balanceResponsibilityRequest.BalanceResponsibleParty.Value },
            { "actor_supplier", balanceResponsibilityRequest.EnergySupplier.Value },
            { "grid_area_code", balanceResponsibilityRequest.GridAreaCode.Value },
            { "valid_from", balanceResponsibilityRequest.ValidFrom.ToDateTimeOffset().ToString("u") },
            { "valid_to", balanceResponsibilityRequest.ValidTo?.ToDateTimeOffset().ToString("u") ?? string.Empty },
        };
    }
}
