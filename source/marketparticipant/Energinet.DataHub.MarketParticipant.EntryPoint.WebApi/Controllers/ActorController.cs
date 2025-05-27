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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Energinet.DataHub.Core.App.Common.Abstractions.Users;
using Energinet.DataHub.MarketParticipant.Application.Commands;
using Energinet.DataHub.MarketParticipant.Application.Commands.Actors;
using Energinet.DataHub.MarketParticipant.Application.Commands.Delegations;
using Energinet.DataHub.MarketParticipant.Application.Security;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Delegations;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Revision;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Security;
using Energinet.DataHub.RevisionLog.Integration.WebApi;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Controllers;

[ApiController]
[Route("actor")]
public sealed class ActorController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IUserContext<FrontendUser> _userContext;

    public ActorController(IMediator mediator, IUserContext<FrontendUser> userContext)
    {
        _mediator = mediator;
        _userContext = userContext;
    }

    [HttpGet]
    [EnableRevision(RevisionActivities.AllActorsRetrieved, typeof(Actor))]
    public async Task<ActionResult<IEnumerable<ActorDto>>> GetActorsAsync()
    {
        var getAllActorsCommand = new GetAllActorsCommand();

        var response = await _mediator
            .Send(getAllActorsCommand)
            .ConfigureAwait(false);

        return Ok(response.Actors);
    }

    [HttpGet("{actorId:guid}")]
    [EnableRevision(RevisionActivities.ActorRetrieved, typeof(Actor), "actorId")]
    public async Task<ActionResult<ActorDto>> GetSingleActorAsync(Guid actorId)
    {
        var getSingleActorCommand = new GetSingleActorCommand(actorId);

        var response = await _mediator
            .Send(getSingleActorCommand)
            .ConfigureAwait(false);

        return Ok(response.Actor);
    }

    [HttpPost]
    [AuthorizeUser(PermissionId.ActorsManage)]
    [EnableRevision(RevisionActivities.ActorCreated, typeof(Actor), "actorId")]
    public async Task<ActionResult<Guid>> CreateActorAsync(CreateActorDto actorDto)
    {
        if (!_userContext.CurrentUser.IsFas)
            return Unauthorized();

        var createActorCommand = new CreateActorCommand(actorDto);

        var response = await _mediator
            .Send(createActorCommand)
            .ConfigureAwait(false);

        return Ok(response.ActorId);
    }

    [HttpPut("{actorId:guid}")]
    [AuthorizeUser(PermissionId.ActorsManage)]
    [EnableRevision(RevisionActivities.ActorEdited, typeof(Actor), "actorId")]
    public async Task<ActionResult> UpdateActorAsync(Guid actorId, ChangeActorDto changeActor)
    {
        if (!_userContext.CurrentUser.IsFasOrAssignedToActor(actorId))
            return Unauthorized();

        var updateActorCommand = new UpdateActorCommand(actorId, changeActor);

        await _mediator
            .Send(updateActorCommand)
            .ConfigureAwait(false);

        return Ok();
    }

    [HttpPut("{actorId:guid}/name")]
    [AuthorizeUser(PermissionId.ActorsManage)]
    [EnableRevision(RevisionActivities.ActorEdited, typeof(Actor), "actorId")]
    public async Task<ActionResult> UpdateActorNameAsync(Guid actorId, ActorNameDto actorNameDto)
    {
        if (!_userContext.CurrentUser.IsFasOrAssignedToActor(actorId))
            return Unauthorized();

        var updateActorNameCommand = new UpdateActorNameCommand(actorId, actorNameDto);

        await _mediator
            .Send(updateActorNameCommand)
            .ConfigureAwait(false);

        return Ok();
    }

    [HttpGet("{actorId:guid}/credentials")]
    [AuthorizeUser(PermissionId.ActorCredentialsManage)]
    [EnableRevision(RevisionActivities.ActorCredentialsViewed, typeof(Actor), "actorId")]
    public async Task<ActionResult<ActorCredentialsDto>> GetActorCredentialsAsync(Guid actorId)
    {
        if (!_userContext.CurrentUser.IsFasOrAssignedToActor(actorId))
            return Unauthorized();

        var command = new GetActorCredentialsCommand(actorId);

        var result = await _mediator
            .Send(command)
            .ConfigureAwait(false);

        return result is not null
            ? Ok(result.CredentialsDto)
            : NotFound();
    }

    [HttpPost("{actorId:guid}/credentials/certificate")]
    [AuthorizeUser(PermissionId.ActorCredentialsManage)]
    [EnableRevision(RevisionActivities.ActorCertificateAssigned, typeof(Actor), "actorId")]
    [RequestSizeLimit(10485760)]
    public async Task<ActionResult> AssignActorCredentialsAsync(Guid actorId, IFormFile certificate)
    {
        if (!_userContext.CurrentUser.IsFasOrAssignedToActor(actorId))
            return Unauthorized();

        ArgumentNullException.ThrowIfNull(certificate);

        var command = new AssignActorCertificateCommand(actorId, certificate.OpenReadStream());

        await _mediator
            .Send(command)
            .ConfigureAwait(false);

        return Ok();
    }

    [HttpDelete("{actorId:guid}/credentials")]
    [AuthorizeUser(PermissionId.ActorCredentialsManage)]
    [EnableRevision(RevisionActivities.ActorCredentialsRemoved, typeof(Actor), "actorId")]
    public async Task<ActionResult> RemoveActorCredentialsAsync(Guid actorId)
    {
        if (!_userContext.CurrentUser.IsFasOrAssignedToActor(actorId))
            return Unauthorized();

        var command = new RemoveActorCredentialsCommand(actorId);

        await _mediator
            .Send(command)
            .ConfigureAwait(false);

        return Ok();
    }

    [HttpPost("{actorId:guid}/credentials/secret")]
    [AuthorizeUser(PermissionId.ActorCredentialsManage)]
    [EnableRevision(RevisionActivities.ActorClientSecretAssigned, typeof(Actor), "actorId")]
    public async Task<ActionResult<ActorClientSecretDto>> ActorRequestSecretAsync(Guid actorId)
    {
        if (!_userContext.CurrentUser.IsFasOrAssignedToActor(actorId))
            return Unauthorized();

        var command = new ActorRequestSecretCommand(actorId);

        var response = await _mediator
            .Send(command)
            .ConfigureAwait(false);

        return Ok(new ActorClientSecretDto(response.SecretText));
    }

    [HttpGet("{actorId:guid}/audit")]
    [AuthorizeUser(PermissionId.ActorsManage)]
    [EnableRevision(RevisionActivities.ActorAuditLogViewed, typeof(Actor), "actorId")]
    public async Task<ActionResult<IEnumerable<AuditLogDto<ActorAuditedChange>>>> GetAuditAsync(Guid actorId)
    {
        if (!_userContext.CurrentUser.IsFasOrAssignedToActor(actorId))
            return Unauthorized();

        var command = new GetActorAuditLogsCommand(actorId);

        var response = await _mediator
            .Send(command)
            .ConfigureAwait(false);

        return Ok(response.AuditLogs);
    }

    [HttpGet("{actorId:guid}/delegations")]
    [AuthorizeUser(PermissionId.DelegationView)]
    [EnableRevision(RevisionActivities.DelegationsForActorViewed, typeof(Actor), "actorId")]
    public async Task<ActionResult<GetDelegationsForActorResponse>> GetDelegationsForActorAsync(Guid actorId)
    {
        if (!_userContext.CurrentUser.IsFasOrAssignedToActor(actorId))
            return Unauthorized();

        var result = await _mediator
            .Send(new GetDelegationsForActorCommand(actorId))
            .ConfigureAwait(false);

        return Ok(result);
    }

    [HttpPost("delegations")]
    [AuthorizeUser(PermissionId.DelegationManage)]
    [EnableRevision(RevisionActivities.ActorDelegationStarted, typeof(ProcessDelegation))]
    public async Task<ActionResult> CreateDelegationAsync([FromBody] CreateProcessDelegationsDto delegationDto)
    {
        if (!_userContext.CurrentUser.IsFas)
            return Unauthorized();

        var createDelegationCommand = new CreateProcessDelegationCommand(delegationDto);

        await _mediator
            .Send(createDelegationCommand)
            .ConfigureAwait(false);

        return Ok();
    }

    [HttpPut("delegations/{delegationId:guid}")]
    [AuthorizeUser(PermissionId.DelegationManage)]
    [EnableRevision(RevisionActivities.ActorDelegationStopped, typeof(ProcessDelegation), "delegationId")]
    public async Task<ActionResult> StopDelegationAsync(Guid delegationId, [FromBody] StopProcessDelegationDto delegationDto)
    {
        if (!_userContext.CurrentUser.IsFas)
            return Unauthorized();

        var stopMessageDelegationCommand = new StopProcessDelegationCommand(delegationId, delegationDto);

        await _mediator
            .Send(stopMessageDelegationCommand)
            .ConfigureAwait(false);

        return Ok();
    }

    [HttpPost("{actorId:guid}/consolidate")]
    [AuthorizeUser(PermissionId.ActorsManage)]
    [EnableRevision(RevisionActivities.ConsolidateActorsRequest, typeof(ActorConsolidation), "actorId")]
    public async Task<ActionResult> ConsolidateActorsAsync(Guid actorId, [FromBody] ConsolidationRequestDto consolidationRequest)
    {
        if (!_userContext.CurrentUser.IsFas)
            return Unauthorized();

        var scheduleConsolidateActorsCommand = new ScheduleConsolidateActorsCommand(actorId, consolidationRequest);

        await _mediator
            .Send(scheduleConsolidateActorsCommand)
            .ConfigureAwait(false);

        return Ok();
    }

    [HttpGet("consolidations")]
    [EnableRevision(RevisionActivities.AllConsolidationsRetrieved, typeof(ActorConsolidation))]
    public async Task<ActionResult<GetActorConsolidationsResponse>> GetActorConsolidationsAsync()
    {
        var getActorConsolidationsCommand = new GetActorConsolidationsCommand();

        var result = await _mediator
            .Send(getActorConsolidationsCommand)
            .ConfigureAwait(false);

        return Ok(result);
    }
}
