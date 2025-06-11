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

using System.ComponentModel;
using Energinet.DataHub.MarketParticipant.Authorization.Model.Parameters;

namespace Energinet.DataHub.MarketParticipant.Authorization.Model.AccessValidationVerify;

public sealed class MeasurementsAccessValidationVerifyRequest : AccessValidationVerifyRequest
{
    [Browsable(false)]
    public MeasurementsAccessValidationVerifyRequest()
    {
    }

    public required string MeteringPointId { get; init; }

    public override string LoggedEntityType => "MeteringPoint";

    public IReadOnlyCollection<AccessPeriod> Periods { get; init; } = new List<AccessPeriod>();

    public override string LoggedEntityKey => MeteringPointId;

    protected override IEnumerable<SignatureParameter> GetSignatureParamsCore()
    {
        return
        [
            SignatureParameter.FromString(SignatureParamKeys.MeteringPointIdKey, MeteringPointId),
            SignatureParameter.FromAccessPeriods(SignatureParamKeys.AccessPeriodsKey, Periods),
        ];
    }
}
