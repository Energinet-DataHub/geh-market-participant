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
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Infrastructure.Extensions;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Xunit;
using User = Microsoft.Graph.Models.User;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;

public sealed class GraphServiceClientFixture : IAsyncLifetime
{
    private readonly IntegrationTestConfiguration _integrationTestConfiguration = new();
    private readonly List<ExternalUserId> _createdUsers = new();
    private GraphServiceClient? _graphClient;

    public GraphServiceClient Client => _graphClient ?? throw new InvalidOperationException($"{nameof(GraphServiceClientFixture)} is not initialized or has already been disposed.");

    public Task InitializeAsync()
    {
        var integrationTestConfig = new IntegrationTestConfiguration();

        var clientSecretCredential = new ClientSecretCredential(
            integrationTestConfig.B2CSettings.Tenant,
            integrationTestConfig.B2CSettings.ServicePrincipalId,
            integrationTestConfig.B2CSettings.ServicePrincipalSecret);

        _graphClient = new GraphServiceClient(
            clientSecretCredential,
            new[] { "https://graph.microsoft.com/.default" });

        Environment.SetEnvironmentVariable(Settings.B2CTenant.Key, integrationTestConfig.B2CSettings.Tenant);
        Environment.SetEnvironmentVariable(Settings.B2CServicePrincipalId.Key, integrationTestConfig.B2CSettings.ServicePrincipalId);
        Environment.SetEnvironmentVariable(Settings.B2CServicePrincipalSecret.Key, integrationTestConfig.B2CSettings.ServicePrincipalSecret);

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
            catch (Exception ex) when (firstException == null)
#pragma warning restore CA1508
            {
                // Nothing can be done.
                firstException = ex;
            }
        }

        _createdUsers.Clear();
        _graphClient = null;

        if (firstException != null)
            throw firstException;
    }

    public async Task<ExternalUserId> CreateUserAsync(
        string testEmail,
        IEnumerable<ObjectIdentity> identities)
    {
        var newUser = CreateTestUserModel(testEmail);
        newUser.Identities!.Clear();
        newUser.Identities!.AddRange(identities);
        newUser.OtherMails = new List<string> { testEmail };

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
