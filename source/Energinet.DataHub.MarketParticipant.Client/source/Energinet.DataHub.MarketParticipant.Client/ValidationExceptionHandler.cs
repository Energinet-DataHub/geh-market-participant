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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Client.Models;
using Flurl.Http;

namespace Energinet.DataHub.MarketParticipant.Client
{
    internal static class ValidationExceptionHandler
    {
        public static async Task<T> HandleAsync<T>(Func<Task<T>> func)
        {
            try
            {
                return await func().ConfigureAwait(false);
            }
            catch (FlurlHttpException e)
            {
                if (e.StatusCode is int statusCode)
                {
                    if (statusCode == 400)
                    {
                        throw new MarketParticipantException(statusCode, message: await e.GetResponseStringAsync().ConfigureAwait(false));
                    }

                    throw new MarketParticipantException(statusCode, string.Empty);
                }

                throw new MarketParticipantException(408, string.Empty);
            }
        }
    }
}
