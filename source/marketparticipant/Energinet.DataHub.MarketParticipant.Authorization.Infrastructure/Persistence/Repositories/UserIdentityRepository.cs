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
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Energinet.DataHub.MarketParticipant.Domain.Services.ActiveDirectory;
using Energinet.DataHub.MarketParticipant.Authorization.Infrastructure.Extensions;
using Energinet.DataHub.MarketParticipant.Authorization.Infrastructure.Options;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Microsoft.Kiota.Abstractions;
using AuthenticationMethod = Energinet.DataHub.MarketParticipant.Domain.Model.Users.Authentication.AuthenticationMethod;
using EmailAddress = Energinet.DataHub.MarketParticipant.Domain.Model.EmailAddress;
using User = Microsoft.Graph.Models.User;
using UserIdentity = Energinet.DataHub.MarketParticipant.Domain.Model.Users.UserIdentity;

namespace Energinet.DataHub.MarketParticipant.Authorization.Infrastructure.Persistence.Repositories;

public sealed class UserIdentityRepository : IUserIdentityRepository
{
    private readonly GraphServiceClient _graphClient;
    private readonly IOptions<AzureB2COptions> _options;
    private readonly IUserIdentityAuthenticationService _userIdentityAuthenticationService;
    private readonly IUserPasswordGenerator _passwordGenerator;

    private readonly string[] _selectors =
    {
        "id",
        "userType",
        "displayName",
        "givenName",
        "surname",
        "identities",
        "mobilePhone",
        "createdDateTime",
        "accountEnabled",
        "userPrincipalName",
        "otherMails"
    };

    public UserIdentityRepository(
        GraphServiceClient graphClient,
        IOptions<AzureB2COptions> options,
        IUserIdentityAuthenticationService userIdentityAuthenticationService,
        IUserPasswordGenerator passwordGenerator)
    {
        _graphClient = graphClient;
        _options = options;
        _userIdentityAuthenticationService = userIdentityAuthenticationService;
        _passwordGenerator = passwordGenerator;
    }

    public async Task<UserIdentity?> GetAsync(ExternalUserId externalId)
    {
        ArgumentNullException.ThrowIfNull(externalId);

        var user = (await _graphClient
            .Users[externalId.Value.ToString()]
            .GetAsync(x => x.QueryParameters.Select = _selectors).ConfigureAwait(false))!;

        return IsMember(user) ? Map(user) : null;
    }

    public async Task<UserIdentity?> FindIdentityReadyForOpenIdSetupAsync(ExternalUserId externalId)
    {
        ArgumentNullException.ThrowIfNull(externalId);

        try
        {
            var user = (await _graphClient
                .Users[externalId.Value.ToString()]
                .GetAsync(x => x.QueryParameters.Select = _selectors)
                .ConfigureAwait(false))!;

            // TODO: Check issuer is pp nets
            var userWithOpenIdConnect = user is { UserType: "Member", Identities: { } } &&
                                        user.Identities.Any(ident => ident.SignInType == "federated");

            var userEmail = user.OtherMails?.FirstOrDefault();

            return userWithOpenIdConnect && userEmail != null ? Map(user, userEmail) : null;
        }
        catch (ODataError ex) when (ex.ResponseStatusCode == 404)
        {
            return null;
        }
    }

    public async Task<UserIdentity?> GetAsync(EmailAddress email)
    {
        ArgumentNullException.ThrowIfNull(email);

        var user = await GetBySignInEmailAsync(email).ConfigureAwait(false);
        return user != null && IsMember(user) ? Map(user) : null;
    }

    public async Task<IEnumerable<UserIdentity>> GetUserIdentitiesAsync(IEnumerable<ExternalUserId> externalIds)
    {
        var ids = externalIds.Distinct();
        var result = new List<UserIdentity>();

        foreach (var segment in ids.Chunk(15))
        {
            var usersRequest = await _graphClient.Users
                .GetAsync(x =>
                {
                    x.QueryParameters.Select = _selectors;
                    x.QueryParameters.Filter = $"id in ({string.Join(",", segment.Select(s => $"'{s}'"))})";
                })
                .ConfigureAwait(false);

            var users = await usersRequest!
                .IteratePagesAsync<User, UserCollectionResponse>(_graphClient)
                .ConfigureAwait(false);

            result.AddRange(users.Where(IsMember).Select(u => Map(u)));
        }

        return result;
    }

    public async Task<IEnumerable<UserIdentity>> SearchUserIdentitiesAsync(string? searchText, bool? accountEnabled)
    {
        var filters = new List<string>();

        if (accountEnabled.HasValue)
        {
            filters.Add($"accountEnabled eq {(accountEnabled.Value ? "true" : "false")}");
        }

        var request = await _graphClient.Users.GetAsync(x =>
        {
            x.QueryParameters.Select = _selectors;
            x.QueryParameters.Count = true;
            if (filters.Count != 0)
                x.QueryParameters.Filter = string.Join(" and ", filters);
        }).ConfigureAwait(false);

        var users = await request!
            .IteratePagesAsync<User, UserCollectionResponse>(_graphClient)
            .ConfigureAwait(false);

        var userIdentities = users.Where(IsMember).Select(u => Map(u));

        if (!string.IsNullOrEmpty(searchText))
        {
            userIdentities = userIdentities
                .Where(userIdentity =>
                    (userIdentity.PhoneNumber != null && userIdentity.PhoneNumber.Number.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                    userIdentity.Email.Address.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    userIdentity.FirstName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    userIdentity.LastName.Contains(searchText, StringComparison.OrdinalIgnoreCase));
        }

        return userIdentities;
    }

    public async Task<ExternalUserId> CreateAsync(UserIdentity userIdentity)
    {
        ArgumentNullException.ThrowIfNull(userIdentity);
        ArgumentNullException.ThrowIfNull(userIdentity.PhoneNumber);

        // It is not possible to create a user and specify the Authentication property.
        // The code is therefore forced to create a user first, then update the property.
        // If the second call fails, we end up with a user that is created without MFA, which is very bad.
        // Therefore, the initial create operation must have the account disabled.
        var createdUser = await CheckCreatedUserAsync(userIdentity.Email).ConfigureAwait(false);
        if (createdUser == null)
        {
            createdUser = await _graphClient.Users
                .PostAsync(CreateUserModel(userIdentity))
                .ConfigureAwait(false);
        }
        else
        {
            await _graphClient
                .Users[createdUser.Id]
                .PatchAsync(CreateUserModel(userIdentity))
                .ConfigureAwait(false);
        }

        var externalUserId = new ExternalUserId(createdUser!.Id!);

        await _userIdentityAuthenticationService
            .AddAuthenticationAsync(externalUserId, userIdentity.Authentication)
            .ConfigureAwait(false);

        var employeeId = userIdentity.SharedId.ToString();

        await _graphClient
            .Users[createdUser.Id]
            .PatchAsync(new User
            {
                AccountEnabled = true,
                Department = employeeId // Cannot use relevant User.EmployeeId as MS thought it was a brilliant idea to limit it to 16 chars, so it cannot fit a Guid.
            })
            .ConfigureAwait(false);

        return externalUserId;
    }

    public async Task UpdateUserAsync(UserIdentity userIdentity)
    {
        ArgumentNullException.ThrowIfNull(userIdentity);

        try
        {
            // The Authentication model is more strict regarding phone number validation, so we update that one first.
            if (await FindAuthenticationMethodIdAsync(userIdentity.Id).ConfigureAwait(false) is { } authenticationId && !string.IsNullOrWhiteSpace(authenticationId))
            {
                await _graphClient
                    .Users[userIdentity.Id.Value.ToString()]
                    .Authentication
                    .PhoneMethods[authenticationId]
                    .PatchAsync(new PhoneAuthenticationMethod
                    {
                        PhoneNumber = userIdentity.PhoneNumber!.Number,
                    }).ConfigureAwait(false);
            }

            await _graphClient
                .Users[userIdentity.Id.Value.ToString()]
                .PatchAsync(new User
                {
                    GivenName = userIdentity.FirstName,
                    Surname = userIdentity.LastName,
                    MobilePhone = userIdentity.PhoneNumber?.Number,
                }).ConfigureAwait(false);
        }
        catch (ODataError e) when (e.Error?.Code == "invalidPhoneNumber")
        {
            throw new ValidationException("Phone number cannot be used with 2FA").WithErrorCode("user.authentication.invalid_phone");
        }
    }

    public Task AssignUserLoginIdentitiesAsync(UserIdentity userIdentity)
    {
        ArgumentNullException.ThrowIfNull(userIdentity);

        return _graphClient
            .Users[userIdentity.Id.ToString()]
            .PatchAsync(new User
            {
                Identities = userIdentity.LoginIdentities.Select(loginIdentity => new ObjectIdentity
                {
                    SignInType = loginIdentity.SignInType,
                    Issuer = loginIdentity.Issuer,
                    IssuerAssignedId = loginIdentity.IssuerAssignedId
                }).ToList()
            });
    }

    public async Task DeleteAsync(ExternalUserId externalUserId)
    {
        ArgumentNullException.ThrowIfNull(externalUserId);

        for (var i = 0; i < 15; i++)
        {
            try
            {
                await _graphClient
                    .Users[externalUserId.Value.ToString()]
                    .DeleteAsync()
                    .ConfigureAwait(false);

                return;
            }
            catch (ODataError ex) when (ex.ResponseStatusCode == 404)
            {
                await Task.Delay(500).ConfigureAwait(false);
            }
        }

        throw new InvalidOperationException($"Could not delete user {externalUserId.Value}.");
    }

    public Task EnableUserAccountAsync(ExternalUserId externalUserId)
    {
        ArgumentNullException.ThrowIfNull(externalUserId);

        return _graphClient
            .Users[externalUserId.Value.ToString()]
            .PatchAsync(new User
            {
                AccountEnabled = true
            });
    }

    public Task DisableUserAccountAsync(UserIdentity userIdentity)
    {
        ArgumentNullException.ThrowIfNull(userIdentity);

        return _graphClient
            .Users[userIdentity.Id.ToString()]
            .PatchAsync(new User
            {
                AccountEnabled = false,

                // Because of the way Azure B2C works, need to remove all external identities for AccountEnabled to work properly.
                Identities = userIdentity.LoginIdentities
                    .Where(loginIdentity => loginIdentity.SignInType == "emailAddress")
                    .Select(loginIdentity => new ObjectIdentity
                    {
                        SignInType = loginIdentity.SignInType,
                        Issuer = loginIdentity.Issuer,
                        IssuerAssignedId = loginIdentity.IssuerAssignedId
                    })
                    .ToList()
            });
    }

    private static UserIdentity Map(User user, string? emailAddress = null)
    {
        var userEmailAddress = emailAddress ?? user
            .Identities!
            .Where(ident => ident.SignInType == "emailAddress")
            .Select(ident => ident.IssuerAssignedId!)
            .Single();

        return new UserIdentity(
            new ExternalUserId(user.Id!),
            new EmailAddress(userEmailAddress),
            user.AccountEnabled == true ? UserIdentityStatus.Active : UserIdentityStatus.Inactive,
            user.GivenName ?? user.DisplayName!,
            user.Surname ?? string.Empty,
            string.IsNullOrWhiteSpace(user.MobilePhone) ? null : new PhoneNumber(user.MobilePhone),
            user.CreatedDateTime!.Value,
            AuthenticationMethod.Undetermined,
            user.Identities!.Select(Map).ToList());
    }

    private static LoginIdentity Map(ObjectIdentity identity)
    {
        return new LoginIdentity(
            identity.SignInType!,
            identity.Issuer!,
            identity.IssuerAssignedId!);
    }

    private static bool IsMember(User user)
    {
        return user is { UserType: "Member", Identities: { } } &&
               user.Identities.Any(ident => ident.SignInType == "emailAddress");
    }

    private async Task<string?> FindAuthenticationMethodIdAsync(ExternalUserId userId)
    {
        var collection = await _graphClient
            .Users[userId.ToString()]
            .Authentication
            .PhoneMethods
            .GetAsync(configuration => configuration.Options = new List<IRequestOption>
            {
                NotFoundRetryHandlerOptionFactory.CreateNotFoundRetryHandlerOption()
            })
            .ConfigureAwait(false);

        var phoneMethods = await collection!
            .IteratePagesAsync<PhoneAuthenticationMethod, PhoneAuthenticationMethodCollectionResponse>(_graphClient)
            .ConfigureAwait(false);

        return phoneMethods
            .FirstOrDefault(method => method.PhoneType == AuthenticationPhoneType.Mobile)?
            .Id;
    }

    private async Task<User?> GetBySignInEmailAsync(EmailAddress email)
    {
        ArgumentNullException.ThrowIfNull(email);

        var usersRequest = await _graphClient
            .Users
            .GetAsync(x =>
            {
                x.QueryParameters.Select = _selectors;
                x.QueryParameters.Filter = $"identities/any(id:id/issuer eq '{_options.Value.Tenant}' and id/issuerAssignedId eq '{email.Address}')";
            })
            .ConfigureAwait(false);

        var users = await usersRequest!
            .IteratePagesAsync<User, UserCollectionResponse>(_graphClient)
            .ConfigureAwait(false);

        return users.SingleOrDefault();
    }

    private async Task<User?> CheckCreatedUserAsync(EmailAddress email)
    {
        var user = await GetBySignInEmailAsync(email).ConfigureAwait(false);
        if (user == null)
            return null;

        if (!IsMember(user))
            throw new NotSupportedException($"Found existing user for '{email}', but UserType is incorrect.");

        if (user.AccountEnabled == true)
            throw new NotSupportedException($"Found existing user for '{email}', but account is already enabled.");

        return user;
    }

    private User CreateUserModel(UserIdentity userIdentity)
    {
        return new User
        {
            AccountEnabled = false,
            DisplayName = userIdentity.FullName,
            GivenName = userIdentity.FirstName,
            Surname = userIdentity.LastName,
            MobilePhone = userIdentity.PhoneNumber!.Number,
            PasswordProfile = new PasswordProfile
            {
                ForceChangePasswordNextSignIn = true,
                Password = _passwordGenerator.GenerateRandomPassword()
            },
            Identities = new List<ObjectIdentity>
            {
                new()
                {
                    SignInType = "emailAddress",
                    Issuer = _options.Value.Tenant,
                    IssuerAssignedId = userIdentity.Email.Address
                }
            }
        };
    }
}
