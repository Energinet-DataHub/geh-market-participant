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
using System.Text.Json.Serialization;
using Energinet.DataHub.Core.App.WebApp.Middleware;
using Energinet.DataHub.Core.App.WebApp.SimpleInjector;
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
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
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

            // app.UseMiddleware<JwtTokenMiddleware>();
            // app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.UseSimpleInjector(Container);
            Container.Verify();
        }

        protected override void Configure(IServiceCollection services)
        {
            services.AddControllers()
                .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

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

        protected override void Configure(Container container)
        {
            var openIdUrl = Configuration["FRONTEND_OPEN_ID_URL"] ?? throw new InvalidOperationException(
                "Frontend OpenID URL not found.");
            var audience = Configuration["FRONTEND_SERVICE_APP_ID"] ?? throw new InvalidOperationException(
                "Frontend service app id not found.");

            // Container.AddJwtTokenSecurity(openIdUrl, audience);
        }
    }
}
