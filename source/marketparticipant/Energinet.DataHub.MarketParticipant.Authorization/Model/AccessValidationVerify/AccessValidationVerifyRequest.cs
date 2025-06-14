﻿// Copyright 2020 Energinet DataHub A/S
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

using Energinet.DataHub.MarketParticipant.Authorization.Model.AccessValidationRequests;
using Energinet.DataHub.MarketParticipant.Authorization.Model.Parameters;

namespace Energinet.DataHub.MarketParticipant.Authorization.Model.AccessValidationVerify;

public abstract class AccessValidationVerifyRequest : ILoggableAccessRequest
{
    public virtual bool LogOnSuccess { get; init; }
    public virtual string LoggedActivity => GetType().Name;
    public abstract string LoggedEntityType { get; }
    public abstract string LoggedEntityKey { get; }
    protected abstract string ContextKey { get; }

    public IReadOnlyList<SignatureParameter> GetSignatureParams()
    {
        return [.. GetSignatureParamsCore(), SignatureParameter.FromString(SignatureParamKeys.ValidationContextKey, ContextKey)];
    }

    protected abstract IEnumerable<SignatureParameter> GetSignatureParamsCore();
}
