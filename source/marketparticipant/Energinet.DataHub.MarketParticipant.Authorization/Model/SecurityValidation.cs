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

using System.Security.Claims;

namespace Energinet.DataHub.MarketParticipant.Authorization.Model
{
    public sealed class SecurityValidation
    {
        private const string MarketRolesClaim = "marketroles";

        public SecurityValidation(IEnumerable<Claim>? claims)
        {
            Claims = claims;
        }

        public IEnumerable<Claim>? Claims { get; }

        public EicFunction GetMarketRole()
        {
            if (Claims == null)
            {
                throw new InvalidOperationException("Claims are null.");
            }

            var claimsList = Claims.ToList(); // Avoid multiple enumerations by materializing the collection.

            var marketRolesClaim = claimsList.SingleOrDefault(c => c.Type == MarketRolesClaim)?.Value
                ?? throw new InvalidOperationException($"Claim of type '{MarketRolesClaim}' not found.");

            if (!Enum.TryParse(marketRolesClaim, true, out EicFunction marketRole))
            {
                throw new InvalidOperationException($"Invalid market role value: {marketRolesClaim}");
            }

            return marketRole;
        }
    }
}
