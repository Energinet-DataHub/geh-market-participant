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
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Extensions;
using Energinet.DataHub.MarketParticipant.Infrastructure.Services.ActiveDirectory;
using Microsoft.Graph;
using AuthenticationMethod = Energinet.DataHub.MarketParticipant.Domain.Model.Users.Authentication.AuthenticationMethod;
using EmailAddress = Energinet.DataHub.MarketParticipant.Domain.Model.EmailAddress;
using User = Microsoft.Graph.User;
using UserIdentity = Energinet.DataHub.MarketParticipant.Domain.Model.Users.UserIdentity;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;

public sealed class UserIdentityRepository : IUserIdentityRepository
{
    private readonly GraphServiceClient _graphClient;
    private readonly IUserIdentityAuthenticationService _userIdentityAuthenticationService;

    private readonly Expression<Func<User, object>> _selectForMapping = user => new
    {
        user.Id,
        user.UserType,
        user.GivenName,
        user.Surname,
        user.Identities,
        user.MobilePhone,
        user.CreatedDateTime,
        user.AccountEnabled
    };

    public UserIdentityRepository(
        GraphServiceClient graphClient,
        IUserIdentityAuthenticationService userIdentityAuthenticationService)
    {
        _graphClient = graphClient;
        _userIdentityAuthenticationService = userIdentityAuthenticationService;
    }

    public async Task<UserIdentity?> GetAsync(ExternalUserId externalId)
    {
        ArgumentNullException.ThrowIfNull(externalId);

        var user = await _graphClient
            .Users[externalId.Value.ToString()]
            .Request()
            .Select(_selectForMapping)
            .GetAsync()
            .ConfigureAwait(false);

        return IsMember(user) ? Map(user) : null;
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
                .Request()
                .Filter($"id in ({string.Join(",", segment.Select(x => $"'{x}'"))})")
                .Select(_selectForMapping)
                .GetAsync()
                .ConfigureAwait(false);

            var users = await usersRequest
                .IteratePagesAsync(_graphClient)
                .ConfigureAwait(false);

            result.AddRange(users.Where(IsMember).Select(Map));
        }

        return result;
    }

    public async Task<IEnumerable<UserIdentity>> SearchUserIdentitiesAsync(string? searchText, bool? accountEnabled)
    {
        var filters = new List<string>();

        if (accountEnabled.HasValue)
        {
            var formattedValue = accountEnabled.Value ? "true" : "false";
            filters.Add($"accountEnabled eq {formattedValue}");
        }

        var queryOptions = new List<Option>
        {
            new HeaderOption("ConsistencyLevel", "eventual"),
            new QueryOption("$count", "true"),
        };

        var request = _graphClient.Users.Request(queryOptions);

        if (filters.Any())
        {
            request = request.Filter(string.Join(" and ", filters));
        }

        var collection = await request
            .Select(_selectForMapping)
            .GetAsync()
            .ConfigureAwait(false);

        var users = await collection
            .IteratePagesAsync(_graphClient)
            .ConfigureAwait(false);

        var userIdentities = users.Where(IsMember).Select(Map);

        if (!string.IsNullOrEmpty(searchText))
        {
            userIdentities = userIdentities
                .Where(userIdentity =>
                    (userIdentity.PhoneNumber != null &&
                    userIdentity.PhoneNumber.Number.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                    userIdentity.Email.Address.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    userIdentity.FirstName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    userIdentity.LastName.Contains(searchText, StringComparison.OrdinalIgnoreCase));
        }

        return userIdentities;
    }

    // TODO: Maybe move into separate file.
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
            var newUser = new User
            {
                AccountEnabled = false,
                DisplayName = userIdentity.FullName,
                GivenName = userIdentity.FirstName,
                Surname = userIdentity.LastName,
                MobilePhone = userIdentity.PhoneNumber.Number,
                PasswordProfile = new PasswordProfile
                {
                    ForceChangePasswordNextSignIn = true,
                    Password = Guid.NewGuid().ToString() // TODO: Does not work with password policy.
                },
                Identities = new[]
                {
                    new ObjectIdentity
                    {
                        SignInType = "emailAddress",
                        Issuer = "devDataHubB2C.onmicrosoft.com", // TODO: Must come from config somewhere, or maybe client?
                        IssuerAssignedId = userIdentity.Email.Address
                    }
                }
            };

            createdUser = await _graphClient.Users
                .Request()
                .AddAsync(newUser)
                .ConfigureAwait(false);
        }

        var externalUserId = new ExternalUserId(createdUser.Id);

        await _userIdentityAuthenticationService
            .AddAuthenticationAsync(externalUserId, userIdentity.Authentication)
            .ConfigureAwait(false);

        await _graphClient
            .Users[createdUser.Id]
            .Request()
            .UpdateAsync(new User { AccountEnabled = true })
            .ConfigureAwait(false);

        return externalUserId;
    }

    private static UserIdentity Map(User user)
    {
        var userEmailAddress = user
            .Identities
            .Where(ident => ident.SignInType == "emailAddress")
            .Select(ident => ident.IssuerAssignedId)
            .Single();

        return new UserIdentity(
            new ExternalUserId(user.Id),
            new EmailAddress(userEmailAddress),
            user.AccountEnabled == true ? UserStatus.Active : UserStatus.Inactive,
            user.GivenName,
            user.Surname,
            string.IsNullOrWhiteSpace(user.MobilePhone) ? null : new PhoneNumber(user.MobilePhone),
            user.CreatedDateTime!.Value,
            AuthenticationMethod.Undetermined);
    }

    private static bool IsMember(User user)
    {
        return user.UserType == "Member";
    }

    private async Task<User?> GetBySignInEmailAsync(EmailAddress email)
    {
        ArgumentNullException.ThrowIfNull(email);

        var usersRequest = await _graphClient
            .Users
            .Request()
            // TODO: Must come from config somewhere, or maybe client?
            .Filter($"identities/any(id:id/issuer eq 'devDataHubB2C.onmicrosoft.com' and id/issuerAssignedId eq '{email.Address}')")
            .Select(x => new { x.Id, x.UserType, x.AccountEnabled })
            .GetAsync()
            .ConfigureAwait(false);

        var users = await usersRequest
            .IteratePagesAsync(_graphClient)
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
}
