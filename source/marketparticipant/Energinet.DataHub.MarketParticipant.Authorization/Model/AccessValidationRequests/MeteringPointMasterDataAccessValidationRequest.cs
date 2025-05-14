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

namespace Energinet.DataHub.MarketParticipant.Authorization.Model.AccessValidationRequests
{
    public class MeteringPointMasterDataAccessValidationRequest : AccessValidationRequest
    {
        [JsonConstructor]
        [Browsable(false)]
        public MeteringPointMasterDataAccessValidationRequest()
        {
        }

        public required EicFunction MarketRole { get; set; }

        public required string MeteringPointId { get; set; } = null!;
        public override IReadOnlyList<SignatureParameter> GetSignatureParams()
        {
            return
            [
                SignatureParameter.FromString("MeteringPointId", MeteringPointId),
                SignatureParameter.FromEicFunction("MarketRole", MarketRole)
            ];
        }
    }
}
