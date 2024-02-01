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

using Energinet.DataHub.MarketParticipant.Application.Commands.User;
using FluentValidation;

namespace Energinet.DataHub.MarketParticipant.Application.Validation;

public sealed class InviteUserCommandRuleSet : AbstractValidator<InviteUserCommand>
{
    public InviteUserCommandRuleSet()
    {
        RuleFor(command => command.Invitation)
            .NotNull()
            .ChildRules(invitationRules =>
            {
                invitationRules
                    .RuleFor(invitation => invitation.InvitationUserDetails!.FirstName)
                    .NotEmpty()
                    .MaximumLength(64)
                    .When(x => x.InvitationUserDetails is not null);

                invitationRules
                    .RuleFor(invitation => invitation.InvitationUserDetails!.LastName)
                    .NotEmpty()
                    .MaximumLength(64)
                    .When(x => x.InvitationUserDetails is not null);

                invitationRules
                    .RuleFor(invitation => invitation.InvitationUserDetails!.PhoneNumber)
                    .NotEmpty()
                    .MaximumLength(30)
                    .Matches("^\\+[0-9]+ [0-9]+$")
                    .When(x => x.InvitationUserDetails is not null);

                invitationRules
                    .RuleFor(invitation => invitation.Email)
                    .NotEmpty()
                    .EmailAddress()
                    .MaximumLength(64);

                invitationRules
                    .RuleFor(invitation => invitation.AssignedActor)
                    .NotEmpty();

                invitationRules
                    .RuleFor(invitation => invitation.AssignedRoles)
                    .NotEmpty()
                    .ForEach(userRoleId => userRoleId.NotEmpty());
            });

        RuleFor(i => i.InvitedByUserId)
            .NotEmpty();
    }
}
