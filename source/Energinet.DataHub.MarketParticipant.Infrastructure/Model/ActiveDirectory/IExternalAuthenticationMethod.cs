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

using System.Threading.Tasks;
using Microsoft.Graph;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Model.ActiveDirectory
{
    /// <summary>
    /// Represents an authentication method in Active Directory.
    /// </summary>
    public interface IExternalAuthenticationMethod
    {
        /// <summary>
        /// Assigns the current authentication method to the user configured in the provided builder.
        /// </summary>
        /// <param name="authenticationBuilder">A configured authentication builder for the target user.</param>
        Task AssignAsync(IAuthenticationRequestBuilder authenticationBuilder);

        /// <summary>
        /// Verifies if the current authentication method is already applied to the user configured in the provided builder.
        /// </summary>
        /// <param name="authenticationBuilder">A configured authentication builder for the target user.</param>
        Task<bool> VerifyAsync(IAuthenticationRequestBuilder authenticationBuilder);
    }
}
