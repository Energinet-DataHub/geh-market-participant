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
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;

namespace Energinet.DataHub.MarketParticipant.Domain.Services;

public sealed class UserInvitationService : IUserInvitationService
{
    private readonly IUserRepository _userRepository;
    private readonly IUserIdentityRepository _userIdentityRepository;
    private readonly IEmailEventRepository _emailEventRepository;
    private readonly IOrganizationDomainValidationService _organizationDomainValidationService;
    private readonly IUserInviteAuditLogEntryRepository _userInviteAuditLogEntryRepository;
    private readonly IUserRoleAssignmentAuditLogEntryRepository _userRoleAssignmentAuditLogEntryRepository;

    public UserInvitationService(
        IUserRepository userRepository,
        IUserIdentityRepository userIdentityRepository,
        IEmailEventRepository emailEventRepository,
        IOrganizationDomainValidationService organizationDomainValidationService,
        IUserInviteAuditLogEntryRepository userInviteAuditLogEntryRepository,
        IUserRoleAssignmentAuditLogEntryRepository userRoleAssignmentAuditLogEntryRepository)
    {
        _userRepository = userRepository;
        _userIdentityRepository = userIdentityRepository;
        _emailEventRepository = emailEventRepository;
        _organizationDomainValidationService = organizationDomainValidationService;
        _userInviteAuditLogEntryRepository = userInviteAuditLogEntryRepository;
        _userRoleAssignmentAuditLogEntryRepository = userRoleAssignmentAuditLogEntryRepository;
    }

    public async Task InviteUserAsync(UserInvitation invitation, UserId invitationSentByUserId)
    {
        ArgumentNullException.ThrowIfNull(invitation);

        await _organizationDomainValidationService
            .ValidateUserEmailInsideOrganizationDomainsAsync(invitation.AssignedActor, invitation.Email)
            .ConfigureAwait(false);

        var invitedUser = await GetUserAsync(invitation.Email).ConfigureAwait(false);
        if (invitedUser == null)
        {
            var userIdentity = new UserIdentity(
                invitation.Email,
                invitation.FirstName,
                invitation.LastName,
                invitation.PhoneNumber,
                invitation.RequiredAuthentication);

            var userIdentityId = await _userIdentityRepository
                .CreateAsync(userIdentity)
                .ConfigureAwait(false);

            invitedUser = new User(userIdentityId);
        }

        invitedUser.SetUserInvitationExpiration();

        var userInviteRoleAssignments = new List<UserRoleAssignment>();

        foreach (var assignedRole in invitation.AssignedRoles)
        {
            var assignment = new UserRoleAssignment(invitation.AssignedActor.Id, assignedRole.Id);
            invitedUser.RoleAssignments.Add(assignment);
            userInviteRoleAssignments.Add(assignment);
        }

        await _emailEventRepository
            .InsertAsync(new EmailEvent(invitation.Email, EmailEventType.UserInvite))
            .ConfigureAwait(false);

        var invitedUserId = await _userRepository
            .AddOrUpdateAsync(invitedUser)
            .ConfigureAwait(false);

        await AuditLogUserInviteAsync(invitedUserId, invitationSentByUserId, invitation).ConfigureAwait(false);
        await AuditLogUserInviteAndUserRoleAssignmentsAsync(invitedUserId, userInviteRoleAssignments, invitationSentByUserId).ConfigureAwait(false);
    }

    private async Task<User?> GetUserAsync(EmailAddress email)
    {
        var invitedIdentity = await _userIdentityRepository
            .GetAsync(email)
            .ConfigureAwait(false);

        return invitedIdentity != null
            ? await _userRepository.GetAsync(invitedIdentity.Id).ConfigureAwait(false)
            : null;
    }

    private async Task AuditLogUserInviteAndUserRoleAssignmentsAsync(
        UserId invitedUserId,
        ICollection<UserRoleAssignment> invitedUserRoleAssignments,
        UserId invitationSentByUserId)
    {
        foreach (var assignment in invitedUserRoleAssignments)
        {
            await _userRoleAssignmentAuditLogEntryRepository.InsertAuditLogEntryAsync(
                invitedUserId,
                new UserRoleAssignmentAuditLogEntry(
                    invitedUserId,
                    assignment.ActorId,
                    assignment.UserRoleId,
                    invitationSentByUserId,
                    DateTimeOffset.UtcNow,
                    UserRoleAssignmentTypeAuditLog.Added)).ConfigureAwait(false);
        }
    }

    private Task AuditLogUserInviteAsync(UserId toUserId, UserId invitationSentByUserId, UserInvitation invitation)
    {
        var userInviteAuditLog = new UserInviteAuditLogEntry(
            toUserId,
            invitationSentByUserId,
            invitation.AssignedActor.Id,
            DateTimeOffset.UtcNow);

        return _userInviteAuditLogEntryRepository
            .InsertAuditLogEntryAsync(userInviteAuditLog);
    }
}
