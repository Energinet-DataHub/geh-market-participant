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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.User;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users.Authentication;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.User;

// TODO: UTs, Validation, Validation UTs
public sealed class InviteUserHandler : IRequestHandler<InviteUserCommand>
{
    private readonly IUserInvitationService _userInvitationService;

    public InviteUserHandler(IUserInvitationService userInvitationService)
    {
        _userInvitationService = userInvitationService;
    }

    public async Task<Unit> Handle(InviteUserCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var phoneNumber = new PhoneNumber(request.Invitation.PhoneNumber);

        var invitation = new UserInvitation(
            new EmailAddress(request.Invitation.Email),
            request.Invitation.FirstName,
            request.Invitation.LastName,
            phoneNumber,
            new SmsAuthenticationMethod(phoneNumber),
            request.Invitation.AssignedActor,
            request.Invitation.AssignedRoles
                .Select(roleId => new UserRoleId(roleId))
                .ToList());

        await _userInvitationService
            .InviteUserAsync(invitation)
            .ConfigureAwait(false);

        return Unit.Value;
    }
}
