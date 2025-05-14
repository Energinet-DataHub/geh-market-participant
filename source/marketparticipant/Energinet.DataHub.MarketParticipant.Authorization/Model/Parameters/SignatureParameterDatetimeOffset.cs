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

namespace Energinet.DataHub.MarketParticipant.Authorization.Model.Parameters;

public class SignatureParameterDatetimeOffset : SignatureParameter
{
    // <summary>
    // Initializes a new instance of the <see cref="SignatureParameterString"/> for <see cref="string"/>classes.
    // </summary>
    // <param name="value">The string value.</param>
    internal SignatureParameterDatetimeOffset(DateTimeOffset value)
    {
        var ticks = value.UtcTicks; // Preserves precision without time zone conversion
        var offsetMinutes = (short)value.Offset.TotalMinutes; // Preserve offset in minutes
        var array = new byte[10];

        var ticksBytes = BitConverter.GetBytes(ticks);
        var offsetBytes = BitConverter.GetBytes(offsetMinutes);

        Buffer.BlockCopy(ticksBytes, 0, array, 0, ticksBytes.Length);
        Buffer.BlockCopy(offsetBytes, 0, array, ticksBytes.Length, offsetBytes.Length);

        ParameterData = array;
    }

    // <inheritdoc />
    internal override byte[] ParameterData { get; }
}
