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
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Energinet.DataHub.Core.FunctionApp.TestCommon;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;

public sealed class KeyClientFixture // TODO: IAsyncLifetime
{
    public string KeyName { get; } = $"IntegrationTestTokenKey-{Guid.NewGuid()}";

    public KeyClient KeyClient { get; private set; } = null!;

    public Task InitializeAsync()
    {
        var keyVaultUri = GetKeyVaultUri();
        KeyClient = new KeyClient(keyVaultUri, new DefaultAzureCredential());

        var rsaKeyOptions = CreateTestKeyOptions();
        return KeyClient.CreateRsaKeyAsync(rsaKeyOptions);
    }

    public Task DisposeAsync()
    {
        return KeyClient.StartDeleteKeyAsync(KeyName);
    }

    private static Uri GetKeyVaultUri()
    {
        var integrationTestConfiguration = new ConfigurationBuilder()
            .AddJsonFile("integrationtest.local.settings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        return new Uri(integrationTestConfiguration.GetValue("AZURE_KEYVAULT_URL"));
    }

    private CreateRsaKeyOptions CreateTestKeyOptions()
    {
        return new CreateRsaKeyOptions(KeyName);
    }
}
