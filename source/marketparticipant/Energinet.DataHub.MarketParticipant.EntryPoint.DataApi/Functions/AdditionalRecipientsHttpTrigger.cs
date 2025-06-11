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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.DataApi.Functions;

internal sealed class AdditionalRecipientsHttpTrigger
{
    [Function("GetAdditionalRecipientsAsync")]
#pragma warning disable CA1822
    public Task<IActionResult> GetAdditionalRecipientsAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "metering-point/{meteringPointId}/additionalRecipients")]
        HttpRequest httpRequest,
        string meteringPointId)
#pragma warning restore CA1822
    {
        ArgumentNullException.ThrowIfNull(httpRequest);
        return Task.FromResult<IActionResult>(new NoContentResult());
    }
}
