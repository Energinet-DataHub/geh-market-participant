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
using Energinet.DataHub.MarketParticipant.Application.Handlers.User;
using Energinet.DataHub.MarketParticipant.Common.Configuration;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;

public abstract class WebApiIntegrationTestsBase : WebApplicationFactory<Startup>
{
    private readonly MarketParticipantDatabaseFixture _fixture;

    protected WebApiIntegrationTestsBase(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;

        GetUserPermissionsHandler.TestWorkaround = TestWorkaroundAsync;
    }

    public static string TestBackendAppId => "7C39AF16-AEA0-4B00-B4DB-D3E7B2D90A2E";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseSetting(Settings.SqlDbConnectionString.Key, _fixture.DatabaseManager.ConnectionString);
        builder.UseSetting(Settings.ServiceBusTopicName.Key, "fake_value");
        builder.UseSetting(Settings.ExternalOpenIdUrl.Key, "fake_value");
        builder.UseSetting(Settings.InternalOpenIdUrl.Key, "fake_value");
        builder.UseSetting(Settings.BackendAppId.Key, TestBackendAppId);
        builder.UseSetting(Settings.TokenKeyVault.Key, "fake_value");
        builder.UseSetting(Settings.TokenKeyName.Key, "fake_value");

        builder.UseSetting(Settings.B2CTenant.Key, Guid.NewGuid().ToString());
        builder.UseSetting(Settings.B2CServicePrincipalNameId.Key, Guid.NewGuid().ToString());
        builder.UseSetting(Settings.B2CServicePrincipalNameSecret.Key, "fake_value");
    }

    private static Task<IEnumerable<UserIdentity>> TestWorkaroundAsync(IEnumerable<ExternalUserId> arg)
    {
        return Task.FromResult(arg.Select(externalUserId => new UserIdentity(
            externalUserId,
            "fake_value",
            new EmailAddress("fake@value"),
            null,
            DateTimeOffset.Now,
            true)));
    }
}
