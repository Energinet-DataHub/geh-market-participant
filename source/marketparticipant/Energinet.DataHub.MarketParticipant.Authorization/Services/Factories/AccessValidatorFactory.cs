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

using Energinet.DataHub.MarketParticipant.Authorization.AccessValidation;
using Energinet.DataHub.MarketParticipant.Authorization.Model;
using Energinet.DataHub.MarketParticipant.Authorization.Services.AccessValidators;

namespace Energinet.DataHub.MarketParticipant.Authorization.Services.Factories
{
    internal sealed class AccessValidatorFactory
    {
        public static IAccessValidator GetAccessValidator(AccessValidationRequest? request)
        {
            ArgumentNullException.ThrowIfNull(request);

            switch (request.GetType())
            {
                case Type t when t == typeof(MeteringPointMasterDataAccessValidationRequest):
                    return new MeteringPointMasterDataAccessValidation((MeteringPointMasterDataAccessValidationRequest)request);
                default:
                    throw new NotImplementedException($"No access validator found for {request.GetType()}");
            }
        }
    }
}
