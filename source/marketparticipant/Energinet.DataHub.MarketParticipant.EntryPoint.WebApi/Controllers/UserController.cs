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
using Energinet.DataHub.MarketParticipant.Application.Commands;
using Energinet.DataHub.MarketParticipant.Application.Commands.User;
using Energinet.DataHub.MarketParticipant.Application.Security;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Controllers;

[ApiController]
[Route("user")]
public class UserController : ControllerBase
{
    private readonly IExternalTokenValidator _externalTokenValidator;
    private readonly IUserContext<FrontendUser> _userContext;
    private readonly IMediator _mediator;

    public UserController(
        IExternalTokenValidator externalTokenValidator,
        IUserContext<FrontendUser> userContext,
        IMediator mediator)
    {
        _externalTokenValidator = externalTokenValidator;
        _userContext = userContext;
        _mediator = mediator;
    }

    private enum IdentityUserPermission
    {
        None,
        AssignedToActor,
        AdministratedByActor,
    }

    [HttpGet("actors")]
    [AllowAnonymous]
    public async Task<ActionResult<GetActorsAssociatedWithExternalUserIdResponse>> GetAssociatedUserActorsAsync(string externalToken)
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
            .Send(new GetActorsAssociatedWithExternalUserIdCommand(externalUserId))
            .ConfigureAwait(false);

        return Ok(associatedActors);
    }

    [HttpGet("{userId:guid}")]
    [AuthorizeUser(PermissionId.UsersView, PermissionId.UsersManage)]
    public async Task<ActionResult<GetUserResponse>> GetAsync(Guid userId)
    {
        if (await GetIdentityPermissionForCurrentUserAsync(userId).ConfigureAwait(false) == IdentityUserPermission.None)
            return Unauthorized();

        var command = new GetUserCommand(userId);

        var response = await _mediator
            .Send(command)
            .ConfigureAwait(false);

        return Ok(response);
    }

    [HttpGet("{userId:guid}/actors")]
    [AuthorizeUser(PermissionId.UsersManage)]
    public async Task<ActionResult<GetActorsAssociatedWithUserResponse>> GetUserActorsAsync(Guid userId)
    {
        var associatedActors = await _mediator
            .Send(new GetActorsAssociatedWithUserCommand(userId))
            .ConfigureAwait(false);

        if (_userContext.CurrentUser.IsFas)
            return Ok(associatedActors);

        var allowedActors = associatedActors
            .ActorIds
            .Where(_userContext.CurrentUser.IsAssignedToActor)
            .ToList();

        return Ok(associatedActors with
        {
            ActorIds = allowedActors
        });
    }

    [HttpGet("{userId:guid}/audit")]
    [AuthorizeUser(PermissionId.UsersManage)]
    public async Task<ActionResult<IEnumerable<AuditLogDto<UserAuditedChange>>>> GetAuditAsync(Guid userId)
    {
        var command = new GetUserAuditLogsCommand(userId);

        var response = await _mediator
            .Send(command)
            .ConfigureAwait(false);

        return Ok(response.AuditLogs);
    }

    [HttpPut("{userId:guid}/useridentity")]
    [AuthorizeUser(PermissionId.UsersManage)]
    public async Task<ActionResult> UpdateUserIdentityAsync(
        Guid userId,
        UserIdentityUpdateDto userIdentityUpdateDto)
    {
        if (await GetIdentityPermissionForCurrentUserAsync(userId).ConfigureAwait(false) == IdentityUserPermission.None)
            return Unauthorized();

        var command = new UpdateUserIdentityCommand(userIdentityUpdateDto, userId);

        await _mediator.Send(command).ConfigureAwait(false);
        return Ok();
    }

    [HttpGet("{userId:guid}/userprofile")]
    [AuthorizeUser]
    public async Task<ActionResult<GetUserProfileResponse>> GetUserProfileAsync(Guid userId)
    {
        if (userId != _userContext.CurrentUser.UserId)
            return Unauthorized();

        var command = new GetUserProfileCommand(userId);

        var userProfile = await _mediator
            .Send(command)
            .ConfigureAwait(false);

        return Ok(userProfile);
    }

    [HttpPut("{userId:guid}/userprofile")]
    [AuthorizeUser]
    public async Task<ActionResult> UpdateUserProfileAsync(
        Guid userId,
        UserProfileUpdateDto userProfileUpdateDto)
    {
        if (userId != _userContext.CurrentUser.UserId)
            return Unauthorized();

        ArgumentNullException.ThrowIfNull(userProfileUpdateDto);

        var command = new UpdateUserIdentityCommand(
            new UserIdentityUpdateDto(
                userProfileUpdateDto.FirstName,
                userProfileUpdateDto.LastName,
                userProfileUpdateDto.PhoneNumber),
            userId);

        await _mediator.Send(command).ConfigureAwait(false);
        return Ok();
    }

    [HttpPost("initiate-mitid-signup")]
    public async Task<ActionResult> InitiateMitIdSignupAsync()
    {
        var command = new InitiateMitIdSignupCommand(_userContext.CurrentUser.UserId);

        await _mediator
            .Send(command)
            .ConfigureAwait(false);

        return Ok();
    }

    [HttpPut("{userId:guid}/deactivate")]
    [AuthorizeUser(PermissionId.UsersManage)]
    public async Task<ActionResult> DeactivateAsync(Guid userId)
    {
        var identityUserPermission = await GetIdentityPermissionForCurrentUserAsync(userId).ConfigureAwait(false);

        if (identityUserPermission is IdentityUserPermission.None)
        {
            return Unauthorized();
        }

        await _mediator
            .Send(new DeactivateUserCommand(userId, identityUserPermission == IdentityUserPermission.AdministratedByActor))
            .ConfigureAwait(false);

        return Ok();
    }

    [HttpPut("{userId:guid}/reset-2fa")]
    [AuthorizeUser(PermissionId.UsersManage)]
    public async Task<ActionResult> ResetTwoFactorAuthenticationAsync(Guid userId)
    {
        if (await GetIdentityPermissionForCurrentUserAsync(userId).ConfigureAwait(false) == IdentityUserPermission.None)
            return Unauthorized();

        var command = new ResetUserTwoFactorAuthenticationCommand(userId);

        await _mediator
            .Send(command)
            .ConfigureAwait(false);

        return Ok();
    }

    [HttpGet("{emailAddress}/exists")]
    [AuthorizeUser(PermissionId.UsersManage)]
    public async Task<ActionResult<bool>> CheckEmailExistsAsync(string emailAddress)
    {
        var command = new CheckEmailExistsCommand(emailAddress);

        var result = await _mediator
            .Send(command)
            .ConfigureAwait(false);

        return Ok(result);
    }

    private static Guid GetExternalUserId(IEnumerable<Claim> claims)
    {
        var userIdClaim = claims.Single(claim => claim.Type == JwtRegisteredClaimNames.Sub);
        return Guid.Parse(userIdClaim.Value);
    }

    private async Task<IdentityUserPermission> GetIdentityPermissionForCurrentUserAsync(Guid userId)
    {
        if (_userContext.CurrentUser.IsFas)
            return IdentityUserPermission.AdministratedByActor;

        var associatedActors = await _mediator
            .Send(new GetActorsAssociatedWithUserCommand(userId))
            .ConfigureAwait(false);

        if (_userContext.CurrentUser.IsAssignedToActor(associatedActors.AdministratedBy))
            return IdentityUserPermission.AdministratedByActor;

        if (associatedActors.ActorIds.Any(_userContext.CurrentUser.IsAssignedToActor))
            return IdentityUserPermission.AssignedToActor;

        return IdentityUserPermission.None;
    }
}
