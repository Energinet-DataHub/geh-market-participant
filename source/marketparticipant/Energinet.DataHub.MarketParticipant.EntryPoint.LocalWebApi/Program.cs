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

using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.LocalWebApi
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var host = Host
                .CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(builder =>
                {
                    builder
                        .UseStartup<NoAuthStartup>()
                        .ConfigureServices(s =>
                        {
                            WebApi.Startup.EnableIntegrationTestKeys = true;
                            s.RemoveAll<IUserIdentityRepository>();
                            s.AddScoped<IUserIdentityRepository, InMemoryUserIdentityRepository>();
                        });
                })
                .Build();

            await host.RunAsync().ConfigureAwait(false);
        }
    }
}
