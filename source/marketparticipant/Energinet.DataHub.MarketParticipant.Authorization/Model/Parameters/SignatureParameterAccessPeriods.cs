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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Authorization.Helpers;

namespace Energinet.DataHub.MarketParticipant.Authorization.Model.Parameters
{
    public class SignatureParameterAccessPeriods : SignatureParameter
    {
        // <summary>
        // Initializes a new instance of the <see cref="SignatureParameterString"/> for <see cref="string"/>classes.
        // </summary>
        // <param name="value">The string value.</param>
        internal SignatureParameterAccessPeriods(IEnumerable<AccessPeriod> accessPeriods)
        {
            var sortedAccessPeriods = accessPeriods.OrderBy(i => i.MeteringPointId).ThenBy(i => i.FromDate);

            var parameterDataSet = new List<byte[]>();

            foreach (var accessPeriod in sortedAccessPeriods)
            {
                parameterDataSet.Add(SignatureParameter.FromString("MeteringPointId", accessPeriod.MeteringPointId).ParameterData);
                parameterDataSet.Add(SignatureParameter.FromDateTimeOffset("FromDate", accessPeriod.FromDate).ParameterData);
                parameterDataSet.Add(SignatureParameter.FromDateTimeOffset("ToDate", accessPeriod.ToDate).ParameterData);
            }

            var arrayLength = parameterDataSet.Sum(i => i.Length);
            var byteArray = new byte[arrayLength];
            var offset = 0;
            foreach (var param in parameterDataSet)
            {
                Buffer.BlockCopy(param, 0, byteArray, offset, param.Length);
                offset += param.Length;
            }

            ParameterData = byteArray;
        }

        // <inheritdoc />
        internal override byte[] ParameterData { get; }
    }
}
