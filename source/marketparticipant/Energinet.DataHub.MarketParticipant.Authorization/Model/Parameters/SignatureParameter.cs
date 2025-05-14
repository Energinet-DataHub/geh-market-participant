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

public abstract partial class SignatureParameter
{
    /// <summary>
    /// Gets the byte array representing the Signature parameter value.
    /// </summary>
    internal abstract byte[] ParameterData { get; }

    public static SignatureParameterLong FromLong(long value) => new(value);
    public static SignatureParameterDatetimeOffset FromDateTimeOffset(DateTimeOffset value) => new(value);
    public static SignatureParameterEicFunction FromEicFunction(EicFunction value) => new(value);
    public static SignatureParameterString FromString(string value) => new(value);
}
