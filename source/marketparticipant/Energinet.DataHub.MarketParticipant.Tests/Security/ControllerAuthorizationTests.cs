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

using System.Linq;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Controllers;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Security;

[UnitTest]
public sealed class ControllerAuthorizationTests
{
    [Fact]
    public void ControllerEndpoint_Exists_MustHaveAuthorizationAttribute()
    {
        // arrange
        var ignoredEndpoints = new[]
        {
            $"{nameof(ActorContactController)}.{nameof(ActorContactController.GetPublicActorContactsAsync)}",
            $"{nameof(ActorController)}.{nameof(ActorController.GetActorsAsync)}",
            $"{nameof(ActorController)}.{nameof(ActorController.GetSingleActorAsync)}",
            $"{nameof(ActorController)}.{nameof(ActorController.GetActorConsolidationsAsync)}",
            $"{nameof(ActorQueryController)}.{nameof(ActorQueryController.GetSelectionActorsAsync)}",
            $"{nameof(GridAreaController)}.{nameof(GridAreaController.GetGridAreasAsync)}",
            $"{nameof(GridAreaController)}.{nameof(GridAreaController.GetGridAreaAsync)}",
            $"{nameof(GridAreaController)}.{nameof(GridAreaController.GetAuditAsync)}",
            $"{nameof(GridAreaOverviewController)}.{nameof(GridAreaOverviewController.GetGridAreaOverviewAsync)}",
            $"{nameof(OrganizationController)}.{nameof(OrganizationController.ListAllAsync)}",
            $"{nameof(OrganizationController)}.{nameof(OrganizationController.GetSingleOrganizationAsync)}",
            $"{nameof(OrganizationController)}.{nameof(OrganizationController.GetActorsAsync)}",
            $"{nameof(PermissionController)}.{nameof(PermissionController.ListAllAsync)}",
            $"{nameof(PermissionController)}.{nameof(PermissionController.GetPermissionAsync)}",
            $"{nameof(UserController)}.{nameof(UserController.InitiateMitIdSignupAsync)}",
            $"{nameof(UserController)}.{nameof(UserController.ResetMitIdAsync)}",
            $"{nameof(UserController)}.{nameof(UserController.GetUserProfileAsync)}",
            $"{nameof(UserController)}.{nameof(UserController.UpdateUserProfileAsync)}",
            $"{nameof(AuditIdentityController)}.{nameof(AuditIdentityController.GetAsync)}",
            $"{nameof(AuditIdentityController)}.{nameof(AuditIdentityController.GetMultipleAsync)}",
            $"{nameof(TokenController)}.{nameof(TokenController.ExchangeDownloadTokenAsync)}",
            $"{nameof(TokenController)}.{nameof(TokenController.CreateDownloadTokenAsync)}",
        };

        // act
        var endpointsMissingAuthorization =
            typeof(OrganizationController).Assembly.GetTypes()
                .Where(x => x.IsSubclassOf(typeof(ControllerBase))).SelectMany(controllerType => controllerType.GetMethods()
                    .Where(httpMethod =>
                        httpMethod.GetCustomAttributes(typeof(AllowAnonymousAttribute), false).Length == 0 &&
                        httpMethod.GetCustomAttributes(typeof(AuthorizeUserAttribute), false).Length == 0 &&
                        (httpMethod.GetCustomAttributes(typeof(HttpGetAttribute), false).Length != 0 ||
                         httpMethod.GetCustomAttributes(typeof(HttpPostAttribute), false).Length != 0 ||
                         httpMethod.GetCustomAttributes(typeof(HttpPutAttribute), false).Length != 0 ||
                         httpMethod.GetCustomAttributes(typeof(HttpDeleteAttribute), false).Length != 0)))
                .Select(x => $"{x.DeclaringType!.Name}.{x.Name}")
                .Except(ignoredEndpoints)
                .ToList();

        // assert
        Assert.False(endpointsMissingAuthorization.Count != 0, $"Following endpoints are missing auth:\n\t{string.Join("\n\t", endpointsMissingAuthorization)}");
    }
}
