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
using Energinet.DataHub.MarketParticipant.Common;
using Energinet.DataHub.MarketParticipant.Common.SimpleInjector;
using Energinet.DataHub.MarketParticipant.Domain;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence;
using Energinet.DataHub.MarketParticipant.Infrastructure.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using SimpleInjector;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Common
{
    [UnitTest]
    public sealed class StartupBaseTests
    {
        [Fact]
        public async Task Startup_ConfigureServices_Verifies()
        {
            // Arrange
            Environment.SetEnvironmentVariable("AZURE_B2C_TENANT", Guid.NewGuid().ToString());
            Environment.SetEnvironmentVariable("AZURE_B2C_SPN_ID", "fake_value");
            Environment.SetEnvironmentVariable("AZURE_B2C_SPN_SECRET", "fake_value");
            Environment.SetEnvironmentVariable("AZURE_B2C_BACKEND_OBJECT_ID", "fake_value");
            Environment.SetEnvironmentVariable("AZURE_B2C_BACKEND_SPN_OBJECT_ID", "fake_value");
            Environment.SetEnvironmentVariable("AZURE_B2C_BACKEND_ID", "fake_value");
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(BuildConfig());
            await using var target = new TestOfStartupBase();

            // Act
            target.ConfigureServices(serviceCollection);
            await using var serviceProvider = serviceCollection.BuildServiceProvider();
            serviceProvider.UseSimpleInjector(target.Container);

            // Assert
            target.Container.Verify();
        }

        [Fact]
        public async Task Startup_ConfigureServices_CallsConfigureContainer()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(BuildConfig());
            var configureContainerMock = new Mock<Action>();
            await using var target = new TestOfStartupBase { ConfigureContainer = configureContainerMock.Object };

            // Act
            target.ConfigureServices(serviceCollection);

            // Assert
            configureContainerMock.Verify(x => x(), Times.Once);
        }

        private static IConfiguration BuildConfig()
        {
            return new ConfigurationBuilder().AddEnvironmentVariables().Build();
        }

        private sealed class TestOfStartupBase : StartupBase
        {
            public Action? ConfigureContainer { get; init; }

            protected override void Configure(IServiceCollection services)
            {
            }

            protected override void ConfigureSimpleInjector(IServiceCollection services)
            {
                var descriptor = new ServiceDescriptor(
                    typeof(IFunctionActivator),
                    typeof(SimpleInjectorActivator),
                    ServiceLifetime.Singleton);

                services.Replace(descriptor);

                services.AddSimpleInjector(Container, x =>
                {
                    x.DisposeContainerWithServiceProvider = false;
                    x.AddLogging();
                });
            }

            protected override void Configure(Container container)
            {
                AddMockConfiguration(container);
                ConfigureContainer?.Invoke();
            }

            private static void AddMockConfiguration(Container container)
            {
                container.Options.AllowOverridingRegistrations = true;
                container.Register(() => new Mock<IMarketParticipantDbContext>().Object, Lifestyle.Scoped);
                container.Register(() => new Mock<IUnitOfWorkProvider>().Object);
                container.RegisterSingleton(() => new Mock<IMarketParticipantServiceBusClient>().Object);
            }
        }
    }
}
