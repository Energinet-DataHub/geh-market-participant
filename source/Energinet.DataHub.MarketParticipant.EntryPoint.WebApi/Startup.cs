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

using System.Text.Json.Serialization;
using Energinet.DataHub.Core.App.Common.Diagnostics.HealthChecks;
using Energinet.DataHub.Core.App.WebApp.Authentication;
using Energinet.DataHub.Core.App.WebApp.Authorization;
using Energinet.DataHub.Core.App.WebApp.Diagnostics.HealthChecks;
using Energinet.DataHub.Core.App.WebApp.SimpleInjector;
using Energinet.DataHub.MarketParticipant.Common.Configuration;
using Energinet.DataHub.MarketParticipant.Common.Extensions;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using SimpleInjector;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi
{
    public sealed class Startup : Common.StartupBase
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint(
                "/swagger/v1/swagger.json",
                "Energinet.DataHub.MarketParticipant.EntryPoint.WebApi v1"));

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();

                // Health check
                endpoints.MapLiveHealthChecks();
                endpoints.MapReadyHealthChecks();
            });

            app.UseSimpleInjector(Container);
            Container.Verify();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            Initialize(_configuration, services);
        }

        protected override void Configure(IConfiguration configuration, IServiceCollection services)
        {
            services
                .AddControllers()
                .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

            var openIdUrl = configuration.GetSetting(Settings.FrontendOpenIdUrl);
            var audience = configuration.GetSetting(Settings.FrontendOpenIdAudience);
            services.AddJwtBearerAuthentication(openIdUrl, audience);
            services.AddPermissionAuthorization();

            var serviceBusConnectionString = configuration.GetSetting(Settings.ServiceBusHealthCheckConnectionString);
            var serviceBusTopicName = configuration.GetSetting(Settings.ServiceBusTopicName);

            services.AddApplicationInsightsTelemetry();

            // Health check
            services
                .AddHealthChecks()
                .AddLiveCheck()
                .AddDbContextCheck<MarketParticipantDbContext>()
                .AddAzureServiceBusTopic(serviceBusConnectionString, serviceBusTopicName);

            services.AddSwaggerGen(c =>
            {
                c.SupportNonNullableReferenceTypes();
                c.SwaggerDoc(
                    "v1",
                    new OpenApiInfo
                    {
                        Title = "Energinet.DataHub.MarketParticipant.EntryPoint.WebApi",
                        Version = "v1"
                    });

                var securitySchema = new OpenApiSecurityScheme
                {
                    Description =
                        "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer", },
                };

                c.AddSecurityDefinition("Bearer", securitySchema);

                var securityRequirement = new OpenApiSecurityRequirement { { securitySchema, new[] { "Bearer" } } };

                c.AddSecurityRequirement(securityRequirement);
            });

            services.AddTransient<IMiddlewareFactory>(_ => new SimpleInjectorMiddlewareFactory(Container));
        }

        protected override void Configure(IConfiguration configuration, Container container)
        {
        }

        protected override void ConfigureSimpleInjector(IServiceCollection services)
        {
            services.AddSimpleInjector(Container, options =>
            {
                options
                    .AddAspNetCore()
                    .AddControllerActivation();

                options.AddLogging();
            });

            services.UseSimpleInjectorAspNetRequestScoping(Container);
        }
    }
}
