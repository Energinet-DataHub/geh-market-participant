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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
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
    private readonly IUserInviteAuditLogRepository _userInviteAuditLogRepository;
    private readonly IUserIdentityAuditLogRepository _userIdentityAuditLogRepository;
    private readonly IUnitOfWorkProvider _unitOfWorkProvider;
    private readonly IUserStatusCalculator _userStatusCalculator;

    public UserInvitationService(
        IUserRepository userRepository,
        IUserIdentityRepository userIdentityRepository,
        IEmailEventRepository emailEventRepository,
        IOrganizationDomainValidationService organizationDomainValidationService,
        IUserInviteAuditLogRepository userInviteAuditLogRepository,
        IUserIdentityAuditLogRepository userIdentityAuditLogRepository,
        IUnitOfWorkProvider unitOfWorkProvider,
        IUserStatusCalculator userStatusCalculator)
    {
        _userRepository = userRepository;
        _userIdentityRepository = userIdentityRepository;
        _emailEventRepository = emailEventRepository;
        _organizationDomainValidationService = organizationDomainValidationService;
        _userInviteAuditLogRepository = userInviteAuditLogRepository;
        _userIdentityAuditLogRepository = userIdentityAuditLogRepository;
        _unitOfWorkProvider = unitOfWorkProvider;
        _userStatusCalculator = userStatusCalculator;
    }

    public async Task InviteUserAsync(UserInvitation invitation, UserId invitationSentByUserId)
    {
        ArgumentNullException.ThrowIfNull(invitation);

        var mailEventType = EmailEventType.UserAssignedToActor;
        var userIdentityModified = false;

        var invitedUser = await GetUserAsync(invitation.Email).ConfigureAwait(false);
        if (invitedUser == null)
        {
            await _organizationDomainValidationService
                .ValidateUserEmailInsideOrganizationDomainsAsync(invitation.AssignedActor, invitation.Email)
                .ConfigureAwait(false);

            var sharedId = new SharedUserReferenceId();

            var userIdentity = new UserIdentity(
                sharedId,
                invitation.Email,
                invitation.FirstName,
                invitation.LastName,
                invitation.PhoneNumber,
                invitation.RequiredAuthentication);

            var userIdentityId = await _userIdentityRepository
                .CreateAsync(userIdentity)
                .ConfigureAwait(false);

            invitedUser = new User(invitation.AssignedActor.Id, sharedId, userIdentityId);
            invitedUser.ActivateUserExpiration();

            mailEventType = EmailEventType.UserInvite;
            userIdentityModified = true;
        }

        foreach (var assignedRole in invitation.AssignedRoles)
        {
            var assignment = new UserRoleAssignment(invitation.AssignedActor.Id, assignedRole.Id);
            invitedUser.RoleAssignments.Add(assignment);
        }

        var uow = await _unitOfWorkProvider
            .NewUnitOfWorkAsync()
            .ConfigureAwait(false);

        await using (uow.ConfigureAwait(false))
        {
            var invitedUserId = await _userRepository
                .AddOrUpdateAsync(invitedUser)
                .ConfigureAwait(false);

            await _emailEventRepository
                .InsertAsync(new EmailEvent(invitation.Email, mailEventType))
                .ConfigureAwait(false);

            var auditIdentity = new AuditIdentity(invitationSentByUserId);

            if (userIdentityModified)
            {
                await AuditLogUserIdentityAsync(invitedUserId, auditIdentity, invitation).ConfigureAwait(false);
            }

            await _userInviteAuditLogRepository
                .AuditAsync(invitedUserId, auditIdentity, invitation.AssignedActor.Id)
                .ConfigureAwait(false);

            await uow.CommitAsync().ConfigureAwait(false);
        }
    }

    public async Task ReInviteUserAsync(User user, UserId invitationSentByUserId)
    {
        ArgumentNullException.ThrowIfNull(user);

        var userIdentity = await _userIdentityRepository.GetAsync(user.ExternalId).ConfigureAwait(false);

        NotFoundValidationException.ThrowIfNull(
            userIdentity,
            user.ExternalId.Value,
            $"The specified user identity {user.ExternalId} was not found.");

        var userStatus = _userStatusCalculator.CalculateUserStatus(user, userIdentity);
        if (userStatus != UserStatus.Invited && userStatus != UserStatus.InviteExpired)
        {
            throw new ValidationException($"The current user invitation for user {user.Id} is not expired and cannot be re-invited.")
                .WithErrorCode("user.invite.not_expired");
        }

        user.ActivateUserExpiration();

        await _userIdentityRepository
            .EnableUserAccountAsync(userIdentity.Id)
            .ConfigureAwait(false);

        var uow = await _unitOfWorkProvider
            .NewUnitOfWorkAsync()
            .ConfigureAwait(false);

        await using (uow.ConfigureAwait(false))
        {
            await _userRepository
                .AddOrUpdateAsync(user)
                .ConfigureAwait(false);

            await _emailEventRepository
                .InsertAsync(new EmailEvent(userIdentity.Email, EmailEventType.UserInvite))
                .ConfigureAwait(false);

            await _userInviteAuditLogRepository
                .AuditAsync(user.Id, new AuditIdentity(invitationSentByUserId), user.AdministratedBy)
                .ConfigureAwait(false);

            await uow.CommitAsync().ConfigureAwait(false);
        }
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

    private async Task AuditLogUserIdentityAsync(UserId invitedUserId, AuditIdentity invitationSentBy, UserInvitation invitation)
    {
        await _userIdentityAuditLogRepository
            .AuditAsync(invitedUserId, invitationSentBy, UserAuditedChange.FirstName, invitation.FirstName, null)
            .ConfigureAwait(false);

        await _userIdentityAuditLogRepository
            .AuditAsync(invitedUserId, invitationSentBy, UserAuditedChange.LastName, invitation.LastName, null)
            .ConfigureAwait(false);

        await _userIdentityAuditLogRepository
            .AuditAsync(invitedUserId, invitationSentBy, UserAuditedChange.PhoneNumber, invitation.PhoneNumber.Number, null)
            .ConfigureAwait(false);
    }
}
