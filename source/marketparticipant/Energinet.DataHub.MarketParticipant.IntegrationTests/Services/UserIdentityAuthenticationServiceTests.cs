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
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users.Authentication;
using Energinet.DataHub.MarketParticipant.Domain.Services.ActiveDirectory;
using Energinet.DataHub.MarketParticipant.Infrastructure;
using Energinet.DataHub.MarketParticipant.Infrastructure.Extensions;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions;
using Xunit;
using Xunit.Categories;
using AuthenticationMethod = Energinet.DataHub.MarketParticipant.Domain.Model.Users.Authentication.AuthenticationMethod;
using ValidationException = FluentValidation.ValidationException;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Services;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class UserIdentityAuthenticationServiceTests
{
    private static readonly PhoneNumber _validPhoneNumber = new("+45 70700000");

    private readonly MarketParticipantDatabaseFixture _databaseFixture;
    private readonly GraphServiceClientFixture _graphServiceClientFixture;

    public UserIdentityAuthenticationServiceTests(
        MarketParticipantDatabaseFixture databaseFixture,
        GraphServiceClientFixture graphServiceClientFixture)
    {
        _databaseFixture = databaseFixture;
        _graphServiceClientFixture = graphServiceClientFixture;
    }

    [Fact]
    public async Task AddAuthenticationAsync_UndeterminedMethod_ThrowsException()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);
        await using var scope = host.BeginScope();
        var target = scope.ServiceProvider.GetRequiredService<IUserIdentityAuthenticationService>();

        var externalUserId = new ExternalUserId(Guid.NewGuid());

        // Act + Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            target.AddAuthenticationAsync(externalUserId, AuthenticationMethod.Undetermined));
    }

    [Fact]
    public async Task AddAuthenticationAsync_UnknownMethod_ThrowsException()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);
        await using var scope = host.BeginScope();
        var target = scope.ServiceProvider.GetRequiredService<IUserIdentityAuthenticationService>();

        var externalUserId = new ExternalUserId(Guid.NewGuid());

        // Act + Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            target.AddAuthenticationAsync(externalUserId, new UnknownAuthenticationMethod()));
    }

    [Fact]
    public async Task AddAuthenticationAsync_NoExistingSmsAuthentication_MethodAddedToUser()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);
        await using var scope = host.BeginScope();
        var target = scope.ServiceProvider.GetRequiredService<IUserIdentityAuthenticationService>();

        var externalUserId = await _graphServiceClientFixture.CreateUserAsync(new RandomlyGeneratedEmailAddress());
        var smsAuthMethod = new SmsAuthenticationMethod(_validPhoneNumber);

        // Act
        await target.AddAuthenticationAsync(externalUserId, smsAuthMethod);

        // Assert
        var actualResponse = (await _graphServiceClientFixture
            .Client
            .Users[externalUserId.ToString()]
            .Authentication
            .PhoneMethods
            .GetAsync())!;

        var phoneAuthMethods = await actualResponse
            .IteratePagesAsync<PhoneAuthenticationMethod, PhoneAuthenticationMethodCollectionResponse>(_graphServiceClientFixture.Client);

        var actualMethods = phoneAuthMethods.ToList();

        Assert.Single(actualMethods);
        Assert.Single(actualMethods, method =>
            method.PhoneNumber == smsAuthMethod.PhoneNumber.Number &&
            method.PhoneType == AuthenticationPhoneType.Mobile);
    }

    [Fact]
    public async Task AddAuthenticationAsync_HasExistingSmsAuthentication_NoExceptionIfValuesIdentical()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);
        await using var scope = host.BeginScope();
        var target = scope.ServiceProvider.GetRequiredService<IUserIdentityAuthenticationService>();

        var externalUserId = await _graphServiceClientFixture.CreateUserAsync(new RandomlyGeneratedEmailAddress());
        var smsAuthMethod = new SmsAuthenticationMethod(_validPhoneNumber);

        await _graphServiceClientFixture
            .Client
            .Users[externalUserId.ToString()]
            .Authentication
            .PhoneMethods
            .PostAsync(
                new PhoneAuthenticationMethod
                {
                    PhoneNumber = _validPhoneNumber.Number,
                    PhoneType = AuthenticationPhoneType.Mobile
                },
                configuration => configuration.Options = new List<IRequestOption>
                {
                    NotFoundRetryHandlerOptionFactory.CreateNotFoundRetryHandlerOption()
                });

        // Act + Assert
        await target.AddAuthenticationAsync(externalUserId, smsAuthMethod);
    }

    [Fact]
    public async Task AddAuthenticationAsync_HasExistingSmsAuthentication_ThrowsExceptionIfValuesDiffers()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);
        await using var scope = host.BeginScope();
        var target = scope.ServiceProvider.GetRequiredService<IUserIdentityAuthenticationService>();

        var externalUserId = await _graphServiceClientFixture.CreateUserAsync(new RandomlyGeneratedEmailAddress());
        var smsAuthMethod = new SmsAuthenticationMethod(_validPhoneNumber);

        await _graphServiceClientFixture
            .Client
            .Users[externalUserId.ToString()]
            .Authentication
            .PhoneMethods
            .PostAsync(
                new PhoneAuthenticationMethod
                {
                    PhoneNumber = "+45 71000000",
                    PhoneType = AuthenticationPhoneType.Mobile
                },
                configuration => configuration.Options = new List<IRequestOption>
                {
                    NotFoundRetryHandlerOptionFactory.CreateNotFoundRetryHandlerOption()
                });

        // Act + Assert
        await Assert.ThrowsAnyAsync<Exception>(() => target.AddAuthenticationAsync(externalUserId, smsAuthMethod));
    }

    [Fact]
    public async Task PhoneNumber_IsInvalid_ThrowsValidationException()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_databaseFixture);
        await using var scope = host.BeginScope();
        var target = scope.ServiceProvider.GetRequiredService<IUserIdentityAuthenticationService>();

        var externalUserId = await _graphServiceClientFixture.CreateUserAsync(new RandomlyGeneratedEmailAddress());
        var smsAuthMethod = new SmsAuthenticationMethod(new PhoneNumber("+45 12345678"));

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(() => target.AddAuthenticationAsync(externalUserId, smsAuthMethod));
    }

    private sealed class UnknownAuthenticationMethod : AuthenticationMethod;
}
