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

using Energinet.DataHub.MarketParticipant.Application;
using Energinet.DataHub.MarketParticipant.Common.ActiveDirectory;
using Energinet.DataHub.MarketParticipant.Common.Email;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Energinet.DataHub.MarketParticipant.Common
{
    public abstract class StartupBase
    {
        public void Initialize(IConfiguration configuration, IServiceCollection services)
        {
            services.AddDbContexts();
            services.AddLogging();
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationPipelineBehaviour<,>));
            services.AddMediatR(config =>
            {
                config.RegisterServicesFromAssemblyContaining<ApplicationAssemblyReference>();
            });

            Configure(configuration, services);

            services.AddApplicationServices();
            services.AddInfrastructureServices();
            services.AddRepositories();
            services.AddDomainServices();
            services.AddUnitOfWorkProvider();
            services.AddAzureAdConfiguration();
            services.AddGraphServiceClient();
            services.AddActiveDirectoryRoles();
            services.AddSendGridEmailSenderClient();
            services.AddEmailConfigRegistration();
            services.AddCvrRegisterConfiguration();
        }

        protected abstract void Configure(IConfiguration configuration, IServiceCollection services);
    }
}
