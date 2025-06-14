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

using Energinet.DataHub.Core.App.FunctionApp.Extensions.Builder;
using Energinet.DataHub.Core.App.FunctionApp.Extensions.DependencyInjection;
using Energinet.DataHub.Core.Logging.LoggingMiddleware;
using Energinet.DataHub.MarketParticipant.EntryPoint.DataApi.Extensions.DependencyInjection;
using Energinet.DataHub.RevisionLog.Integration.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsForIsolatedWorker("mark-part-data-api");
        services.AddHealthChecksForIsolatedWorker();

        services.AddRevisionLogIntegrationModule(context.Configuration);
        services.AddMarketParticipantDataApiModule(context.Configuration);
    })
    .ConfigureLogging((hostingContext, logging) =>
    {
        logging.AddLoggingConfigurationForIsolatedWorker(hostingContext.Configuration);
        logging.SetApplicationInsightLogLevel();
    })
    .Build();

host.Run();
