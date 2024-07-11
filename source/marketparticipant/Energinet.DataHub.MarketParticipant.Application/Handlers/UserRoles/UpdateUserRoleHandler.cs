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
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoles;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services.Rules;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.UserRoles;

public sealed class UpdateUserRoleHandler : IRequestHandler<UpdateUserRoleCommand>
{
    private readonly IUniqueUserRoleNameRuleService _uniqueUserRoleNameRuleService;
    private readonly IAllowedPermissionsForUserRoleRuleService _allowedPermissionsForUserRoleRuleService;
    private readonly IUserRoleRepository _userRoleRepository;

    public UpdateUserRoleHandler(
        IUniqueUserRoleNameRuleService uniqueUserRoleNameRuleService,
        IAllowedPermissionsForUserRoleRuleService allowedPermissionsForUserRoleRuleService,
        IUserRoleRepository userRoleRepository)
    {
        _uniqueUserRoleNameRuleService = uniqueUserRoleNameRuleService;
        _allowedPermissionsForUserRoleRuleService = allowedPermissionsForUserRoleRuleService;
        _userRoleRepository = userRoleRepository;
    }

    public async Task Handle(UpdateUserRoleCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var userRoleToUpdate = await _userRoleRepository.GetAsync(new UserRoleId(request.UserRoleId)).ConfigureAwait(false);
        NotFoundValidationException.ThrowIfNull(userRoleToUpdate, request.UserRoleId);

        if (userRoleToUpdate.Status == UserRoleStatus.Inactive)
            throw new ValidationException($"Cannot update inactive user role '{request.UserRoleUpdateDto.Name}'.");

        userRoleToUpdate.Name = request.UserRoleUpdateDto.Name;
        userRoleToUpdate.Description = request.UserRoleUpdateDto.Description;
        userRoleToUpdate.Status = request.UserRoleUpdateDto.Status;
        userRoleToUpdate.Permissions = request.UserRoleUpdateDto.Permissions.Select(p => (PermissionId)p);

        await _allowedPermissionsForUserRoleRuleService
            .ValidateUserRolePermissionsAsync(userRoleToUpdate)
            .ConfigureAwait(false);

        await _uniqueUserRoleNameRuleService
            .ValidateUserRoleNameAsync(userRoleToUpdate)
            .ConfigureAwait(false);

        await _userRoleRepository
            .UpdateAsync(userRoleToUpdate)
            .ConfigureAwait(false);
    }
}
