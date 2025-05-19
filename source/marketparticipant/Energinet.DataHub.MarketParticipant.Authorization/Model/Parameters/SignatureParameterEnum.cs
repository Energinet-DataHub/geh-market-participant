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

using System.Text;

namespace Energinet.DataHub.MarketParticipant.Authorization.Model.Parameters;

 #pragma warning disable CA1711
public class SignatureParameterEnum<T> : SignatureParameter
 #pragma warning restore CA1711
    where T : Enum
{
    // <summary>
    // Initializes a new instance of the <see cref="SignatureParameterEnum"/> for <see cref="long"/>class.
    // </summary>
    // <param name="value">The long value.</param>
    internal SignatureParameterEnum(T value)
    {
        var nameBytes = Encoding.UTF8.GetBytes(value.GetType().Name);
        var valueBytes = Encoding.UTF8.GetBytes(value.ToString());
        ParameterData = nameBytes.Concat(valueBytes).ToArray();
    }

    // <inheritdoc />
    internal override byte[] ParameterData { get; }
}
