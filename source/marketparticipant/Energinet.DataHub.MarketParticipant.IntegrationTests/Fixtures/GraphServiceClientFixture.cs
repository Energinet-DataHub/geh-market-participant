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
using System.Threading.Tasks;
using Azure.Identity;
using Energinet.DataHub.Core.FunctionApp.TestCommon.Configuration;
using Energinet.DataHub.MarketParticipant.Common.Configuration;
using Energinet.DataHub.MarketParticipant.Domain.Model.ActiveDirectory;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Infrastructure.Extensions;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Xunit;
using User = Microsoft.Graph.Models.User;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;

#pragma warning disable CA1001
public sealed class GraphServiceClientFixture : IAsyncLifetime
#pragma warning restore CA1001
{
    private readonly IntegrationTestConfiguration _integrationTestConfiguration = new();
    private readonly List<ExternalUserId> _createdUsers = new();
    private GraphServiceClient? _graphClient;

    public GraphServiceClient Client => _graphClient ?? throw new InvalidOperationException($"{nameof(GraphServiceClientFixture)} is not initialized or has already been disposed.");

    public Task InitializeAsync()
    {
        var clientSecretCredential = new ClientSecretCredential(
            _integrationTestConfiguration.B2CSettings.Tenant,
            _integrationTestConfiguration.B2CSettings.ServicePrincipalId,
            _integrationTestConfiguration.B2CSettings.ServicePrincipalSecret);

        _graphClient = new GraphServiceClient(
            clientSecretCredential,
            new[]
            {
                "https://graph.microsoft.com/.default"
            });

        Environment.SetEnvironmentVariable(Settings.B2CTenant.Key, _integrationTestConfiguration.B2CSettings.Tenant);
        Environment.SetEnvironmentVariable(Settings.B2CServicePrincipalId.Key, _integrationTestConfiguration.B2CSettings.ServicePrincipalId);
        Environment.SetEnvironmentVariable(Settings.B2CServicePrincipalSecret.Key, _integrationTestConfiguration.B2CSettings.ServicePrincipalSecret);

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (_graphClient == null)
            return;

        Exception? firstException = null;

        foreach (var externalUserId in _createdUsers)
        {
            try
            {
                await _graphClient
                    .Users[externalUserId.ToString()]
                    .DeleteAsync();
            }
            catch (ODataError dataError) when (dataError.ResponseStatusCode == 404)
            {
                // User already deleted.
            }
#pragma warning disable CA1508
#pragma warning disable CA1031
            catch (Exception ex) when (firstException is null)
#pragma warning restore CA1031
            {
                // Nothing can be done.
                firstException = ex;
            }
        }

        _createdUsers.Clear();
        _graphClient = null;

        if (firstException != null)
            throw firstException;
#pragma warning restore CA1508
    }

    public async Task<ExternalUserId> CreateUserAsync(
        string testEmail,
        IEnumerable<ObjectIdentity> identities)
    {
        var newUser = CreateTestUserModel(testEmail);
        newUser.Identities!.Clear();
        newUser.Identities!.AddRange(identities);
        newUser.OtherMails = new List<string>
        {
            testEmail
        };

        var externalUserId = await CreateUserAndAddToCleanUpListAsync(newUser);
        return externalUserId;
    }

    public async Task<ExternalUserId> CreateUserAsync(string testEmail)
    {
        var newUser = CreateTestUserModel(testEmail);

        var externalUserId = await CreateUserAndAddToCleanUpListAsync(newUser);
        return externalUserId;
    }

    public async Task<User?> TryFindExternalUserAsync(string testEmail)
    {
        var usersRequest = await Client
            .Users
            .GetAsync(x =>
            {
                x.QueryParameters.Select = new[]
                {
                    "id",
                    "identities"
                };
                x.QueryParameters.Filter = $"identities/any(id:id/issuer eq '{_integrationTestConfiguration.B2CSettings.Tenant}' and id/issuerAssignedId eq '{testEmail}')";
            })
            .ConfigureAwait(false);

        var users = await usersRequest!
            .IteratePagesAsync<User, UserCollectionResponse>(Client)
            .ConfigureAwait(false);

        var user = users.SingleOrDefault();
        return user;
    }

    public async Task CleanupExternalUserAsync(string testEmail)
    {
        var existingUser = await TryFindExternalUserAsync(testEmail);
        if (existingUser == null)
            return;

        await Client
            .Users[existingUser.Id]
            .DeleteAsync();
    }

    public async Task<ActiveDirectoryAppInformation> GetExistingAppRegistrationAsync(string appId)
    {
        var retrievedApp = (await _graphClient!.Applications //[appRegistrationObjectId.Value.ToString()]
            .GetAsync(x =>
            {
                x.QueryParameters.Select = new[]
                {
                    "appId",
                    "id",
                    "displayName",
                    "appRoles",
                };
                x.QueryParameters.Filter = $"appId eq '{appId}'";
            })
            .ConfigureAwait(false))!;

        var applications = await retrievedApp
            .IteratePagesAsync<Microsoft.Graph.Models.Application, ApplicationCollectionResponse>(Client)
            .ConfigureAwait(false);

        var application = applications.FirstOrDefault() ?? throw new InvalidOperationException("No application found");

        var servicePrincipalCollectionResponse = await _graphClient.ServicePrincipals
            .GetAsync(x =>
            {
                x.QueryParameters.Filter = $"appId eq '{application.AppId}'";
            })
            .ConfigureAwait(false);

        var servicePrincipals = await servicePrincipalCollectionResponse!
            .IteratePagesAsync<ServicePrincipal, ServicePrincipalCollectionResponse>(_graphClient)
            .ConfigureAwait(false);

        var servicePrincipal = servicePrincipals.FirstOrDefault() ?? throw new InvalidOperationException("No service principal found");

        var appRoles = await GetRolesAsync(servicePrincipal.Id!).ConfigureAwait(false);

        return new ActiveDirectoryAppInformation(
            application.AppId!,
            application.Id!,
            application.DisplayName!,
            appRoles);
    }

    private async Task<IEnumerable<ActiveDirectoryRole>> GetRolesAsync(string servicePrincipalObjectId)
    {
        var response = await _graphClient!.ServicePrincipals[servicePrincipalObjectId]
            .AppRoleAssignments
            .GetAsync()
            .ConfigureAwait(false);

        var roles = await response!
            .IteratePagesAsync<AppRoleAssignment, AppRoleAssignmentCollectionResponse>(_graphClient)
            .ConfigureAwait(false);

        if (roles is null)
        {
            throw new InvalidOperationException($"'{nameof(roles)}' is null");
        }

        var roleIds = new List<ActiveDirectoryRole>();
        foreach (var role in roles)
        {
            roleIds.Add(new ActiveDirectoryRole(role.AppRoleId.ToString()!));
        }

        return roleIds;
    }

    private async Task<ExternalUserId> CreateUserAndAddToCleanUpListAsync(User newUser)
    {
        var createdUser = await Client
            .Users
            .PostAsync(newUser)
            .ConfigureAwait(false);

        var externalUserId = new ExternalUserId(createdUser!.Id!);
        _createdUsers.Add(externalUserId);

        return externalUserId;
    }

    private User CreateTestUserModel(string testEmail)
    {
        return new User
        {
            AccountEnabled = false,
            DisplayName = "User Integration Tests (Always safe to delete)",
            GivenName = "Test First Name",
            Surname = "Test Last Name",
            PasswordProfile = new PasswordProfile
            {
                ForceChangePasswordNextSignIn = true,
                Password = Guid.NewGuid().ToString()
            },
            Identities = new List<ObjectIdentity>()
            {
                new()
                {
                    SignInType = "emailAddress",
                    Issuer = _integrationTestConfiguration.B2CSettings.Tenant,
                    IssuerAssignedId = testEmail
                }
            }
        };
    }
}
