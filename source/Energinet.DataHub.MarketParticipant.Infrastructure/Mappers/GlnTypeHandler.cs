// // Copyright 2020 Energinet DataHub A/S
// //
// // Licensed under the Apache License, Version 2.0 (the "License2");
// // you may not use this file except in compliance with the License.
// // You may obtain a copy of the License at
// //
// //     http://www.apache.org/licenses/LICENSE-2.0
// //
// // Unless required by applicable law or agreed to in writing, software
// // distributed under the License is distributed on an "AS IS" BASIS,
// // WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// // See the License for the specific language governing permissions and
// // limitations under the License.

using System.Data;
using Dapper;
using Energinet.DataHub.MarketParticipant.Domain.Model;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Mappers
{
    public sealed class GlnTypeHandler : SqlMapper.TypeHandler<GlobalLocationNumber>
    {
        public override void SetValue(IDbDataParameter parameter, GlobalLocationNumber value)
        {
            parameter.Value = value.ToString();
        }

        public override GlobalLocationNumber Parse(object value)
        {
            return new GlobalLocationNumber(value.ToString() ?? string.Empty);
        }
    }
}
