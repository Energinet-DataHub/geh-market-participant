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
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.UserRoles;

public sealed class UpdateUserRoleHandler : IRequestHandler<UpdateUserRoleCommand>
{
    private readonly IUserRoleRepository _userRoleRepository;

    public UpdateUserRoleHandler(IUserRoleRepository userRoleRepository)
    {
        _userRoleRepository = userRoleRepository;
    }

    public async Task Handle(UpdateUserRoleCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var userRoleToUpdate = await _userRoleRepository.GetAsync(new UserRoleId(request.UserRoleId)).ConfigureAwait(false);
        NotFoundValidationException.ThrowIfNull(userRoleToUpdate, request.UserRoleId);

        if (userRoleToUpdate.Status == UserRoleStatus.Inactive)
            throw new ValidationException($"User role with name {request.UserRoleUpdateDto.Name} is deactivated and can't be updated");

        var userRoleWithSameName = await _userRoleRepository.GetByNameInMarketRoleAsync(request.UserRoleUpdateDto.Name, userRoleToUpdate.EicFunction).ConfigureAwait(false);
        if (userRoleWithSameName != null && userRoleWithSameName.Id.Value != userRoleToUpdate.Id.Value)
        {
            throw new ValidationException($"User role with name {request.UserRoleUpdateDto.Name} already exists in market role");
        }

        userRoleToUpdate.Name = request.UserRoleUpdateDto.Name;
        userRoleToUpdate.Description = request.UserRoleUpdateDto.Description;
        userRoleToUpdate.Status = request.UserRoleUpdateDto.Status;
        userRoleToUpdate.Permissions = request.UserRoleUpdateDto.Permissions.Select(p => (PermissionId)p);
        userRoleToUpdate.ChangedByIdentityId = request.ChangedByUserId;

        await _userRoleRepository
            .UpdateAsync(userRoleToUpdate)
            .ConfigureAwait(false);
    }
}
