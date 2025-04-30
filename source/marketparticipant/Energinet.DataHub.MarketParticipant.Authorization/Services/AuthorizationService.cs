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
using System.Security.Cryptography;
using Energinet.DataHub.MarketParticipant.Domain.Model.Authorization;
using Energinet.DataHub.MarketParticipant.Authorization.Restriction;

namespace Energinet.DataHub.MarketParticipant.Authorization.Services
{
    public sealed class AuthorizationService : IAuthorizationService
    {
        private readonly ECDsa _ecdsa = ECDsa.Create();

        public AuthorizationService()
        {
        }

        // Copied from example. Not sure when it is called.
        public void Dispose()
        {
            _ecdsa.Dispose();
        }

        // Later this task has AuthorizationRestriction and UserIdentification as input
        public async Task<byte[]> CreateSignatureAsync()
        {
            // 1. Call api to make authorization check. (Input: AuthorizationRestriction and UserIdentification)
            // 2. If authorization succesfull: Create a signature (Input: AuthorizationRestriction) if unautorised return null
            // For now just return a static signature
            // Will be later something like this:
            // Var binaryRestriction = restriction.ToByteArray();
            byte[] binaryRestriction = [1, 2, 3, 4];
            var signature = _ecdsa.SignData(binaryRestriction, HashAlgorithmName.SHA256);
            // 3. Return signature - ignore error handling
            return signature;
        }

        public async Task<bool> VerifySignatureAsync(AuthorizationRestriction restriction, byte[] signature)
        {
            // Will be later something like this:
            // Var binaryRestriction = restriction.ToByteArray();
            // For now Static
            byte[] binaryRestriction = [1, 2, 3, 4];
            var isValid = _ecdsa.VerifyData(binaryRestriction, signature, HashAlgorithmName.SHA256);
            return isValid;
        }
    }
}
