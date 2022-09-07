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

using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class JwtController : ControllerBase
    {
        private readonly ILogger<OrganizationController> _logger;
        private readonly IUserIdProvider _userIdProvider;

        public JwtController(ILogger<OrganizationController> logger, IUserIdProvider userIdProvider)
        {
            _logger = logger;
            _userIdProvider = userIdProvider;
        }

        [HttpGet]
        public async Task<IActionResult> GetSubjectAsync()
        {
            return await this.ProcessAsync(
                async () =>
                    {
                        await Task.CompletedTask.ConfigureAwait(false);
                        return Ok(string.Join(",", User.Claims.Select(x => x.Type)));

                        // var userId = _userIdProvider.UserId;
                        // return Ok(userId.ToString());
                    },
                _logger).ConfigureAwait(false);
        }
    }
}
