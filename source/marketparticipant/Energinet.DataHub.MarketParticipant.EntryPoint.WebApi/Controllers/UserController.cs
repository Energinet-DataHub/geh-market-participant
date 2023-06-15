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
using Energinet.DataHub.MarketParticipant.Application.Commands.Query.Actor;
using Energinet.DataHub.MarketParticipant.Application.Commands.User;
using Energinet.DataHub.MarketParticipant.Application.Security;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Extensions;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Controllers;

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
        return await this.ProcessAsync(
            async () =>
            {
                if (string.IsNullOrWhiteSpace(externalToken))
                    return BadRequest();

                var externalJwt = new JwtSecurityToken(externalToken);

                if (!await _externalTokenValidator
                        .ValidateTokenAsync(externalToken)
                        .ConfigureAwait(false))
                {
                    return Unauthorized();
                }

                var externalUserId = GetExternalUserId(externalJwt.Claims);

                var associatedActors = await _mediator
                    .Send(new GetAssociatedUserActorsCommand(externalUserId))
                    .ConfigureAwait(false);

                return Ok(associatedActors);
            },
            _logger).ConfigureAwait(false);
    }

    [HttpGet("{userId:guid}")]
    [AuthorizeUser(PermissionId.UsersView, PermissionId.UsersManage)]
    public async Task<IActionResult> GetAsync(Guid userId)
    {
        return await this.ProcessAsync(
            async () =>
            {
                if (!_userContext.CurrentUser.IsFas)
                {
                    if (!await DoesUserBelongToActorAsync(userId, _userContext.CurrentUser.ActorId).ConfigureAwait(false))
                        return Unauthorized();
                }

                var command = new GetUserCommand(userId);

                var response = await _mediator
                    .Send(command)
                    .ConfigureAwait(false);

                return Ok(response);
            },
            _logger).ConfigureAwait(false);
    }

    [HttpGet("{userId:guid}/actors")]
    [AuthorizeUser(PermissionId.UsersManage)]
    public async Task<IActionResult> GetUserActorsAsync(Guid userId)
    {
        return await this.ProcessAsync(
            async () =>
            {
                var associatedActors = await _mediator
                    .Send(new GetSelectionActorsQueryCommand(userId))
                    .ConfigureAwait(false);

                var associatedActorIds = associatedActors.Actors.Select(actor => actor.Id);

                if (_userContext.CurrentUser.IsFas)
                    return Ok(new GetAssociatedUserActorsResponse(associatedActorIds));

                var allowedActors = new List<Guid>();

                foreach (var actorId in associatedActorIds)
                {
                    if (_userContext.CurrentUser.IsAssignedToActor(actorId))
                        allowedActors.Add(actorId);
                }

                return Ok(new GetAssociatedUserActorsResponse(allowedActors));
            },
            _logger).ConfigureAwait(false);
    }

    [HttpGet("{userId:guid}/auditlogentry")]
    [AuthorizeUser(PermissionId.UsersManage)]
    public async Task<IActionResult> GetAuditLogsAsync(Guid userId)
    {
        return await this.ProcessAsync(
            async () =>
            {
                var command = new GetUserAuditLogsCommand(userId);

                var response = await _mediator
                    .Send(command)
                    .ConfigureAwait(false);

                var filteredUserRoleAssignmentAuditLogs = _userContext.CurrentUser.IsFas
                    ? response.UserRoleAssignmentAuditLogs
                    : response.UserRoleAssignmentAuditLogs.Where(log => log.ActorId == _userContext.CurrentUser.ActorId);

                var filteredUserInviteDetailsAuditLogs = _userContext.CurrentUser.IsFas
                    ? response.InviteAuditLogs
                    : response.InviteAuditLogs.Where(log => log.ActorId == _userContext.CurrentUser.ActorId);

                return Ok(new GetUserAuditLogResponse(filteredUserRoleAssignmentAuditLogs, filteredUserInviteDetailsAuditLogs, response.IdentityAuditLogs));
            },
            _logger).ConfigureAwait(false);
    }

    [HttpPut("{userId:guid}/useridentity")]
    [AuthorizeUser(PermissionId.UsersManage)]
    public async Task<IActionResult> UpdateUserIdentityAsync(
        Guid userId,
        UserIdentityUpdateDto userIdentityUpdateDto)
    {
        return await this.ProcessAsync(
            async () =>
            {
                if (!_userContext.CurrentUser.IsFas)
                {
                    if (!await DoesUserBelongToActorAsync(userId, _userContext.CurrentUser.ActorId).ConfigureAwait(false))
                        return Unauthorized();
                }

                var command = new UpdateUserIdentityCommand(userIdentityUpdateDto, userId);

                await _mediator.Send(command).ConfigureAwait(false);
                return Ok();
            },
            _logger).ConfigureAwait(false);
    }

    [HttpPost("initiate-mitid-signup")]
    public async Task<IActionResult> InitiateMitIdSignupAsync()
    {
        return await this.ProcessAsync(
            async () =>
            {
                var command = new InitiateMitIdSignupCommand(_userContext.CurrentUser.UserId);

                await _mediator
                    .Send(command)
                    .ConfigureAwait(false);

                return Ok();
            },
            _logger).ConfigureAwait(false);
    }

    [HttpPut("{userId:guid}/deactivate")]
    [AuthorizeUser(PermissionId.UsersManage)]
    public async Task<IActionResult> DeactivateAsync(Guid userId)
    {
        return await this.ProcessAsync(
            async () =>
            {
                if (!_userContext.CurrentUser.IsFas)
                {
                    if (!await DoesUserBelongToActorAsync(userId, _userContext.CurrentUser.ActorId).ConfigureAwait(false))
                        return Unauthorized();
                }

                var command = new DeactivateUserCommand(userId, _userContext.CurrentUser.UserId);

                await _mediator
                    .Send(command)
                    .ConfigureAwait(false);

                return Ok();
            },
            _logger).ConfigureAwait(false);
    }

    private static Guid GetExternalUserId(IEnumerable<Claim> claims)
    {
        var userIdClaim = claims.Single(claim => claim.Type == JwtRegisteredClaimNames.Sub);
        return Guid.Parse(userIdClaim.Value);
    }

    private async Task<bool> DoesUserBelongToActorAsync(Guid userId, Guid expectedActorId)
    {
        var associatedActors = await _mediator
            .Send(new GetSelectionActorsQueryCommand(userId))
            .ConfigureAwait(false);

        return associatedActors.Actors.Any(actor => actor.Id == expectedActorId);
    }
}
