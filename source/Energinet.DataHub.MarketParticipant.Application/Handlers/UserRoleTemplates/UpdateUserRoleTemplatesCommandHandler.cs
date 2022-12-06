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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoleTemplates;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.UserRoleTemplates;

public sealed class UpdateUserRoleTemplatesCommandHandler
    : IRequestHandler<UpdateUserRoleTemplatesCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserRoleTemplateRepository _userRoleTemplateRepository;

    public UpdateUserRoleTemplatesCommandHandler(
        IUserRepository userRepository,
        IUserRoleTemplateRepository userRoleTemplateRepository)
    {
        _userRepository = userRepository;
        _userRoleTemplateRepository = userRoleTemplateRepository;
    }

    public async Task<Unit> Handle(
        UpdateUserRoleTemplatesCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await _userRepository
            .GetAsync(new UserId(request.UserId))
            .ConfigureAwait(false);

        if (user == null)
        {
            throw new NotFoundValidationException(request.UserId);
        }

        var assignments = new List<UserRoleAssignment>();
        foreach (var userRoleTemplateAssignment in request.RoleTemplatesDto.UserRoleTemplateAssignments)
        {
            foreach (var userRoleTemplateId in userRoleTemplateAssignment.Value)
            {
                assignments.Add(new UserRoleAssignment(
                    user.Id,
                    userRoleTemplateAssignment.Key,
                    userRoleTemplateId));
            }
        }

        user.RoleAssignments.Clear();
        assignments.ForEach(x => user.RoleAssignments.Add(x));
        await _userRepository.AddOrUpdateAsync(user).ConfigureAwait(false);

        return Unit.Value;
    }
}
