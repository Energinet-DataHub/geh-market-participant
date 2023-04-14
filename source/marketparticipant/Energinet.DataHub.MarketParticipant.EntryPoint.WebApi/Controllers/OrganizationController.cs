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
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.Core.App.Common.Abstractions.Users;
using Energinet.DataHub.MarketParticipant.Application.Commands.Actor;
using Energinet.DataHub.MarketParticipant.Application.Commands.Organization;
using Energinet.DataHub.MarketParticipant.Application.Security;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Extensions;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Security;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OrganizationController : ControllerBase
    {
        private readonly ILogger<OrganizationController> _logger;
        private readonly IMediator _mediator;
        private readonly IUserContext<FrontendUser> _userContext;

        public OrganizationController(ILogger<OrganizationController> logger, IMediator mediator, IUserContext<FrontendUser> userContext)
        {
            _logger = logger;
            _mediator = mediator;
            _userContext = userContext;
        }

        [HttpGet]
        public async Task<IActionResult> ListAllAsync()
        {
            return await this.ProcessAsync(
                async () =>
                    {
                        var organizationId = !_userContext.CurrentUser.IsFas ? _userContext.CurrentUser.OrganizationId : (Guid?)null;
                        var getOrganizationsCommand = new GetOrganizationsCommand(organizationId);

                        var response = await _mediator
                            .Send(getOrganizationsCommand)
                            .ConfigureAwait(false);

                        return Ok(response.Organizations);
                    },
                _logger).ConfigureAwait(false);
        }

        [HttpGet("{organizationId:guid}")]
        public async Task<IActionResult> GetSingleOrganizationAsync(Guid organizationId)
        {
            return await this.ProcessAsync(
                async () =>
                {
                    if (!_userContext.CurrentUser.IsFasOrAssignedToOrganization(organizationId))
                        return Unauthorized();

                    var getSingleOrganizationCommand = new GetSingleOrganizationCommand(organizationId);

                    var response = await _mediator
                        .Send(getSingleOrganizationCommand)
                        .ConfigureAwait(false);

                    return Ok(response.Organization);
                },
                _logger).ConfigureAwait(false);
        }

        [HttpPost]
        [AuthorizeUser(PermissionId.OrganizationsManage)]
        public async Task<IActionResult> CreateOrganizationAsync(CreateOrganizationDto organization)
        {
            return await this.ProcessAsync(
                async () =>
                {
                    if (!_userContext.CurrentUser.IsFas)
                        return Unauthorized();

                    var createOrganizationCommand = new CreateOrganizationCommand(organization);

                    var response = await _mediator
                        .Send(createOrganizationCommand)
                        .ConfigureAwait(false);

                    return Ok(response.OrganizationId.ToString());
                },
                _logger).ConfigureAwait(false);
        }

        [HttpPut("{organizationId:guid}")]
        [AuthorizeUser(PermissionId.OrganizationsManage)]
        public async Task<IActionResult> UpdateOrganizationAsync(
            Guid organizationId,
            ChangeOrganizationDto organization)
        {
            return await this.ProcessAsync(
                async () =>
                {
                    if (!_userContext.CurrentUser.IsFas)
                        return Unauthorized();

                    var updateOrganizationCommand =
                        new UpdateOrganizationCommand(organizationId, organization);

                    await _mediator
                        .Send(updateOrganizationCommand)
                        .ConfigureAwait(false);

                    return Ok();
                },
                _logger).ConfigureAwait(false);
        }

        [HttpGet("{organizationId:guid}/actor")]
        public async Task<IActionResult> GetActorsAsync(Guid organizationId)
        {
            return await this.ProcessAsync(
                async () =>
                {
                    if (!_userContext.CurrentUser.IsFasOrAssignedToOrganization(organizationId))
                        return Unauthorized();

                    var getActorsCommand = new GetActorsCommand(organizationId);

                    var response = await _mediator
                        .Send(getActorsCommand)
                        .ConfigureAwait(false);

                    var filteredActors = response.Actors;

                    if (!_userContext.CurrentUser.IsFas)
                    {
                        filteredActors = filteredActors.Select(actor =>
                        {
                            if (actor.ActorId == _userContext.CurrentUser.ActorId.ToString())
                                return actor;

                            return actor with { Name = new ActorNameDto(string.Empty) };
                        });
                    }

                    return Ok(filteredActors);
                },
                _logger).ConfigureAwait(false);
        }
    }
}