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
using System.Threading.Tasks;
using Energinet.DataHub.Core.App.Common.Abstractions.Users;
using Energinet.DataHub.MarketParticipant.Application.Commands.Actor;
using Energinet.DataHub.MarketParticipant.Application.Security;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Security;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Controllers
{
    [ApiController]
    [Route("actor")]
    public class ActorController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IUserContext<FrontendUser> _userContext;

        public ActorController(IMediator mediator, IUserContext<FrontendUser> userContext)
        {
            _mediator = mediator;
            _userContext = userContext;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ActorDto>>> GetActorsAsync()
        {
            var getAllActorsCommand = new GetAllActorsCommand();

            var response = await _mediator
                .Send(getAllActorsCommand)
                .ConfigureAwait(false);

            return Ok(response.Actors);
        }

        [HttpGet("{actorId:guid}")]
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

        [HttpGet("{actorId:guid}/credentials")]
        [AuthorizeUser(PermissionId.ActorCredentialsManage)]
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

        // TODO: Delete.
        [HttpGet("{actorId:guid}/auditlogs")]
        [AuthorizeUser(PermissionId.ActorsManage)]
        public async Task<ActionResult<GetActorAuditLogsResponseOld>> GetActorAuditLogsAsync(Guid actorId)
        {
            if (!_userContext.CurrentUser.IsFasOrAssignedToActor(actorId))
                return Unauthorized();

            var command = new GetActorAuditLogsCommand(actorId);

            var response = await _mediator
                .Send(command)
                .ConfigureAwait(false);

            var actorAuditLogs = new List<ActorAuditLogDto>();
            var actorContactAuditLogs = new List<ActorContactAuditLogDto>();

            foreach (var auditLog in response.AuditLogs)
            {
                switch (auditLog.Change)
                {
                    case ActorAuditedChange.Name:
                        actorAuditLogs.Add(new ActorAuditLogDto(actorId, auditLog.CurrentValue!, auditLog.PreviousValue!, auditLog.AuditIdentityId, auditLog.Timestamp, ActorChangeType.Name));
                        break;
                    case ActorAuditedChange.Status:
                        actorAuditLogs.Add(new ActorAuditLogDto(actorId, auditLog.CurrentValue!, auditLog.PreviousValue!, auditLog.AuditIdentityId, auditLog.Timestamp, ActorChangeType.Status));
                        break;
                    case ActorAuditedChange.ContactName:
                        actorContactAuditLogs.Add(new ActorContactAuditLogDto(actorId, auditLog.CurrentValue!, auditLog.PreviousValue!, auditLog.AuditIdentityId, auditLog.Timestamp, ContactCategory.Default, ActorContactChangeType.Name));
                        break;
                    case ActorAuditedChange.ContactEmail:
                        actorContactAuditLogs.Add(new ActorContactAuditLogDto(actorId, auditLog.CurrentValue!, auditLog.PreviousValue!, auditLog.AuditIdentityId, auditLog.Timestamp, ContactCategory.Default, ActorContactChangeType.Email));
                        break;
                    case ActorAuditedChange.ContactPhone:
                        actorContactAuditLogs.Add(new ActorContactAuditLogDto(actorId, auditLog.CurrentValue!, auditLog.PreviousValue!, auditLog.AuditIdentityId, auditLog.Timestamp, ContactCategory.Default, ActorContactChangeType.Phone));
                        break;
                    case ActorAuditedChange.ContactCategoryAdded:
                        actorContactAuditLogs.Add(new ActorContactAuditLogDto(actorId, auditLog.CurrentValue!, auditLog.PreviousValue!, auditLog.AuditIdentityId, auditLog.Timestamp, Enum.Parse<ContactCategory>(auditLog.CurrentValue!), ActorContactChangeType.Created));
                        break;
                    case ActorAuditedChange.ContactCategoryRemoved:
                        actorContactAuditLogs.Add(new ActorContactAuditLogDto(actorId, auditLog.CurrentValue!, auditLog.PreviousValue!, auditLog.AuditIdentityId, auditLog.Timestamp, Enum.Parse<ContactCategory>(auditLog.PreviousValue!), ActorContactChangeType.Deleted));
                        break;
                    case ActorAuditedChange.CertificateCredentials:
                        actorAuditLogs.Add(new ActorAuditLogDto(actorId, auditLog.CurrentValue!, auditLog.PreviousValue!, auditLog.AuditIdentityId, auditLog.Timestamp, ActorChangeType.CertificateCredentials));
                        break;
                    case ActorAuditedChange.ClientSecretCredentials:
                        actorAuditLogs.Add(new ActorAuditLogDto(actorId, auditLog.CurrentValue!, auditLog.PreviousValue!, auditLog.AuditIdentityId, auditLog.Timestamp, ActorChangeType.SecretCredentials));
                        break;
                    default:
                        throw new InvalidOperationException("Incorrect audit log change.");
                }
            }

            return Ok(new GetActorAuditLogsResponseOld(actorAuditLogs, actorContactAuditLogs));
        }

        [HttpGet("{actorId:guid}/audit")]
        [AuthorizeUser(PermissionId.ActorsManage)]
        public async Task<ActionResult<GetActorAuditLogsResponse>> GetAuditAsync(Guid actorId)
        {
            if (!_userContext.CurrentUser.IsFasOrAssignedToActor(actorId))
                return Unauthorized();

            var command = new GetActorAuditLogsCommand(actorId);

            var response = await _mediator
                .Send(command)
                .ConfigureAwait(false);

            return Ok(response);
        }
    }
}
