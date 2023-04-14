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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users.Authentication;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Energinet.DataHub.MarketParticipant.Infrastructure;
using Energinet.DataHub.MarketParticipant.Infrastructure.Extensions;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Services.ActiveDirectory;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Moq;
using Xunit;
using Xunit.Categories;
using AuthenticationMethod = Energinet.DataHub.MarketParticipant.Domain.Model.Users.Authentication.AuthenticationMethod;
using EmailAddress = Energinet.DataHub.MarketParticipant.Domain.Model.EmailAddress;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories;

[Collection("IntegrationTest")]
[IntegrationTest]
public sealed class UserIdentityRepositoryTests : IAsyncLifetime
{
    private const string TestUserEmail = "invitation-integration-test@datahub.dk";

    private readonly MarketParticipantDatabaseFixture _databaseFixture;
    private readonly GraphServiceClientFixture _graphServiceClientFixture;

    public UserIdentityRepositoryTests(
        MarketParticipantDatabaseFixture databaseFixture,
        GraphServiceClientFixture graphServiceClientFixture)
    {
        _databaseFixture = databaseFixture;
        _graphServiceClientFixture = graphServiceClientFixture;
    }

    [Fact]
    public async Task CreateAsync_ValidUser_CanBeReadBack()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);
        await using var scope = host.BeginScope();

        var graphServiceClient = scope.GetInstance<GraphServiceClient>();
        var azureIdentityConfig = scope.GetInstance<AzureIdentityConfig>();
        var userIdentityAuthenticationService = scope.GetInstance<IUserIdentityAuthenticationService>();
        var userPasswordGenerator = scope.GetInstance<IUserPasswordGenerator>();

        var target = new UserIdentityRepository(
            graphServiceClient,
            azureIdentityConfig,
            userIdentityAuthenticationService,
            userPasswordGenerator);

        var userIdentity = new Domain.Model.Users.UserIdentity(
            new EmailAddress(TestUserEmail),
            "User Integration Tests",
            "(Always safe to delete)",
            new PhoneNumber("+45 70000000"),
            new SmsAuthenticationMethod(new PhoneNumber("+45 71000000")));

        // Act
        var externalUserId = await target.CreateAsync(userIdentity);

        // Assert
        var actual = await target.GetAsync(externalUserId);
        Assert.NotNull(actual);
        Assert.Equal(userIdentity.Email, actual.Email);
        Assert.Equal(userIdentity.FirstName, actual.FirstName);
        Assert.Equal(userIdentity.LastName, actual.LastName);
        Assert.Equal(userIdentity.PhoneNumber, actual.PhoneNumber);
        Assert.Equal(userIdentity.Status, actual.Status);

        var password = await _graphServiceClientFixture.Client
            .Users[actual.Id.ToString()]
            .Authentication
            .PasswordMethods
            .GetAsync();

        var passwordMethods = await password!.IteratePagesAsync<PasswordAuthenticationMethod>(_graphServiceClientFixture.Client);
        Assert.NotEmpty(passwordMethods);
    }

    [Fact]
    public async Task CreateAsync_UserExistsAndEnabled_ThrowsException()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);
        await using var scope = host.BeginScope();

        var graphServiceClient = scope.GetInstance<GraphServiceClient>();
        var azureIdentityConfig = scope.GetInstance<AzureIdentityConfig>();
        var userIdentityAuthenticationService = scope.GetInstance<IUserIdentityAuthenticationService>();
        var userPasswordGenerator = scope.GetInstance<IUserPasswordGenerator>();

        var target = new UserIdentityRepository(
            graphServiceClient,
            azureIdentityConfig,
            userIdentityAuthenticationService,
            userPasswordGenerator);

        var userIdentity = new Domain.Model.Users.UserIdentity(
            new EmailAddress(TestUserEmail),
            "User Integration Tests",
            "(Always safe to delete)",
            new PhoneNumber("+45 70000000"),
            new SmsAuthenticationMethod(new PhoneNumber("+45 71000000")));

        await target.CreateAsync(userIdentity);

        // Act + Assert
        await Assert.ThrowsAsync<NotSupportedException>(() => target.CreateAsync(userIdentity));
    }

    [Fact]
    public async Task CreateAsync_UserExistsAndDisabled_ReturnsSameId()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);
        await using var scope = host.BeginScope();

        var graphServiceClient = scope.GetInstance<GraphServiceClient>();
        var azureIdentityConfig = scope.GetInstance<AzureIdentityConfig>();
        var userIdentityAuthenticationService = scope.GetInstance<IUserIdentityAuthenticationService>();
        var userPasswordGenerator = scope.GetInstance<IUserPasswordGenerator>();

        var userIdentity = new Domain.Model.Users.UserIdentity(
            new MockedEmailAddress(),
            "User Integration Tests",
            "(Always safe to delete)",
            new PhoneNumber("+45 70000000"),
            new SmsAuthenticationMethod(new PhoneNumber("+45 71000000")));

        var expected = await _graphServiceClientFixture.CreateUserAsync(userIdentity.Email.Address);

        var target = new UserIdentityRepository(
            graphServiceClient,
            azureIdentityConfig,
            userIdentityAuthenticationService,
            userPasswordGenerator);

        // Act
        var actual = await target.CreateAsync(userIdentity);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task CreateAsync_AddAuthenticationFails_UserStaysDeactivated()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);
        await using var scope = host.BeginScope();

        var graphServiceClient = scope.GetInstance<GraphServiceClient>();
        var azureIdentityConfig = scope.GetInstance<AzureIdentityConfig>();
        var userPasswordGenerator = scope.GetInstance<IUserPasswordGenerator>();

        var userIdentityAuthenticationServiceMock = new Mock<IUserIdentityAuthenticationService>();
        userIdentityAuthenticationServiceMock
            .Setup(userIdentityAuthenticationService => userIdentityAuthenticationService.AddAuthenticationAsync(
                It.IsAny<ExternalUserId>(),
                It.IsAny<AuthenticationMethod>()))
            .ThrowsAsync(new TimeoutException());

        var target = new UserIdentityRepository(
            graphServiceClient,
            azureIdentityConfig,
            userIdentityAuthenticationServiceMock.Object,
            userPasswordGenerator);

        var userIdentity = new Domain.Model.Users.UserIdentity(
            new EmailAddress(TestUserEmail),
            "User Integration Tests",
            "(Always safe to delete)",
            new PhoneNumber("+45 70000000"),
            new SmsAuthenticationMethod(new PhoneNumber("+45 71000000")));

        // Act
        await Assert.ThrowsAsync<TimeoutException>(() => target.CreateAsync(userIdentity));

        // Assert
        var actual = await target.GetAsync(userIdentity.Email);
        Assert.NotNull(actual);
        Assert.Equal(UserStatus.Inactive, actual.Status);
    }

    public Task InitializeAsync() => _graphServiceClientFixture.CleanupExternalUserAsync(TestUserEmail);
    public Task DisposeAsync() => _graphServiceClientFixture.CleanupExternalUserAsync(TestUserEmail);
}