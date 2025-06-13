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
using System.Text.Json.Serialization;
using Energinet.DataHub.MarketParticipant.Authorization.Model.Parameters;

namespace Energinet.DataHub.MarketParticipant.Authorization.Model.AccessValidationVerify;

public sealed class MeasurementsYearlySumAccessVerifyRequest : AccessValidationVerifyRequest
{
    [JsonConstructor]
    [Browsable(false)]
    public MeasurementsYearlySumAccessVerifyRequest()
    {
    }

    public required string MeteringPointId { get; init; }

    public IReadOnlyCollection<AccessPeriod> Periods { get; init; } = new List<AccessPeriod>();

    public override string LoggedEntityType => "MeteringPoint";

    public override string LoggedEntityKey => MeteringPointId;

    protected override string ContextKey => SignatureParamContextKeys.MeasurementsYearlySumAccessValidation;

    protected override IEnumerable<SignatureParameter> GetSignatureParamsCore()
    {
        return
        [
            SignatureParameter.FromString(SignatureParamKeys.MeteringPointIdKey, MeteringPointId),
        ];
    }
}
