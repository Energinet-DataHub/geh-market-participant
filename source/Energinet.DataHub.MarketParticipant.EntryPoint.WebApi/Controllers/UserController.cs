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
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Energinet.DataHub.Core.App.Common.Abstractions.Users;
using Energinet.DataHub.Core.App.Common.Security;
using Energinet.DataHub.Core.App.WebApp.Authorization;
using Energinet.DataHub.MarketParticipant.Application.Commands.User;
using Energinet.DataHub.MarketParticipant.Application.Security;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Extensions;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Controllers
{
    [ApiController]
    [Route("user")]
    public class UserController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;
        private readonly IExternalTokenValidator _externalTokenValidator;
        private readonly IUserContext<FrontendUser> _userContext;
        private readonly IMediator _mediator;

        public UserController(
            ILogger<UserController> logger,
            IExternalTokenValidator externalTokenValidator,
            IUserContext<FrontendUser> userContext,
            IMediator mediator)
        {
            _logger = logger;
            _externalTokenValidator = externalTokenValidator;
            _userContext = userContext;
            _mediator = mediator;
        }

        [HttpGet("actors")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAssociatedUserActorsAsync(string externalToken)
        {
            ArgumentNullException.ThrowIfNull(externalToken);

            var externalJwt = new JwtSecurityToken(externalToken);

            return await this.ProcessAsync(
                async () =>
                {
                    if (!await _externalTokenValidator
                            .ValidateTokenAsync(externalToken)
                            .ConfigureAwait(false))
                    {
                        return Unauthorized();
                    }

                    var userId = GetUserId(externalJwt.Claims);

                    var associatedActors = await _mediator
                        .Send(new GetAssociatedUserActorsCommand(userId))
                        .ConfigureAwait(false);

                    return Ok(associatedActors);
                },
                _logger).ConfigureAwait(false);
        }

        [HttpGet("{userId:guid}/actors")]
        [AuthorizeUser(Permission.UsersManage)]
        public async Task<IActionResult> GetUserActorsAsync(Guid userId)
        {
            ArgumentNullException.ThrowIfNull(userId);

            return await this.ProcessAsync(
                async () =>
                {
                    var associatedActors = await _mediator
                        .Send(new GetAssociatedUserActorsCommand(userId))
                        .ConfigureAwait(false);

                    if (_userContext.CurrentUser.IsFas)
                        return Ok(associatedActors);

                    var allowedActors = new List<Guid>();
                    foreach (var actorId in associatedActors.ActorIds)
                    {
                        if (_userContext.CurrentUser.IsAssignedToActor(actorId))
                            allowedActors.Add(actorId);
                    }

                    return Ok(new GetAssociatedUserActorsResponse(allowedActors));
                },
                _logger).ConfigureAwait(false);
        }

        private static Guid GetUserId(IEnumerable<Claim> claims)
        {
            var userIdClaim = claims.Single(claim => claim.Type == JwtRegisteredClaimNames.Sub);
            return Guid.Parse(userIdClaim.Value);
        }
    }
}
