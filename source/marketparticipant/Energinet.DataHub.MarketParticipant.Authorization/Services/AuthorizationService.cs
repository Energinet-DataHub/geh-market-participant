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

using System.Runtime.InteropServices;
using Energinet.DataHub.MarketParticipant.Domain.Model.Authorization;

namespace Energinet.DataHub.MarketParticipant.Authorization.Services
{
    public sealed class AuthorizationService : IAuthorizationService
    {
        public AuthorizationService()
        {
        }

        public async Task<byte[]> CreateSignatureAsync()
        {
           return CreateStaticSignature();
        }

        public async Task<bool> VerifySignatureAsync(AuthorizationRestriction restriction, byte[] signature)
        {
            return CreateStaticSignature().SequenceEqual(signature);
        }

        private byte[] CreateStaticSignature()
        {
            return [1, 2, 3, 4];
        }
    }
}
