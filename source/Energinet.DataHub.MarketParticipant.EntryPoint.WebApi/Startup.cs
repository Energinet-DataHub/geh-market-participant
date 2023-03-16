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
using System.Net.Http;
using System.Text.Json.Serialization;
using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Energinet.DataHub.Core.App.Common.Diagnostics.HealthChecks;
using Energinet.DataHub.Core.App.WebApp.Authentication;
using Energinet.DataHub.Core.App.WebApp.Authorization;
using Energinet.DataHub.Core.App.WebApp.Diagnostics.HealthChecks;
using Energinet.DataHub.Core.App.WebApp.SimpleInjector;
using Energinet.DataHub.MarketParticipant.Application.Security;
using Energinet.DataHub.MarketParticipant.Common.Configuration;
using Energinet.DataHub.MarketParticipant.Common.Extensions;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Security;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using NodaTime;
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

        /// <summary>
        /// Disables validation of external token and CreatedOn limit for KeyVault keys.
        /// This property is intended for testing purposes only.
        /// </summary>
        public static bool EnableIntegrationTestKeys { get; set; }

        public void Configure(
            IApplicationBuilder app,
            IWebHostEnvironment env,
            IHostApplicationLifetime appLifetime)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                Container.Options.EnableAutoVerification = false;
            }

            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint(
                "/swagger/v1/swagger.json",
                "Energinet.DataHub.MarketParticipant.EntryPoint.WebApi v1"));

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseUserMiddleware<FrontendUser>();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers().RequireAuthorization();

                // Health check
                endpoints.MapLiveHealthChecks();
                endpoints.MapReadyHealthChecks();
            });

            app.UseSimpleInjector(Container);

            var internalOpenIdUrl = _configuration.GetSetting(Settings.InternalOpenIdUrl);
            appLifetime?.ApplicationStarted.Register(() => OnApplicationStarted(internalOpenIdUrl));
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

            if (_configuration.GetSetting(Settings.AllowAllTokens))
            {
                services.AddDummyJwtBearerAuthentication();
            }
            else
            {
                var externalOpenIdUrl = configuration.GetSetting(Settings.ExternalOpenIdUrl);
                var internalOpenIdUrl = configuration.GetSetting(Settings.InternalOpenIdUrl);
                var backendAppId = configuration.GetSetting(Settings.BackendAppId);
                services.AddJwtBearerAuthentication(externalOpenIdUrl, internalOpenIdUrl, backendAppId);
            }

            services.AddPermissionAuthorization();

            // Health check
            services
                .AddHealthChecks()
                .AddLiveCheck()
                .AddDbContextCheck<MarketParticipantDbContext>();

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
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                };

                c.AddSecurityDefinition("Bearer", securitySchema);

                var securityRequirement = new OpenApiSecurityRequirement { { securitySchema, new[] { "Bearer" } } };

                c.AddSecurityRequirement(securityRequirement);
            });

            services.AddTransient<IMiddlewareFactory>(_ => new SimpleInjectorMiddlewareFactory(Container));
        }

        protected override void Configure(IConfiguration configuration, Container container)
        {
            ArgumentNullException.ThrowIfNull(container);

            container.RegisterSingleton<IExternalTokenValidator>(() =>
            {
                var externalOpenIdUrl = configuration.GetSetting(Settings.ExternalOpenIdUrl);
                var backendAppId = configuration.GetSetting(Settings.BackendAppId);
                return new ExternalTokenValidator(externalOpenIdUrl, backendAppId);
            });

            container.RegisterSingleton<ISigningKeyRing>(() =>
            {
                var tokenKeyVaultUri = configuration.GetSetting(Settings.TokenKeyVault);
                var tokenKeyName = configuration.GetSetting(Settings.TokenKeyName);

                var tokenCredentials = new DefaultAzureCredential();

                var keyClient = new KeyClient(tokenKeyVaultUri, tokenCredentials);
                return new SigningKeyRing(SystemClock.Instance, keyClient, tokenKeyName);
            });

            container.AddUserAuthentication<FrontendUser, FrontendUserProvider>();
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

#pragma warning disable VSTHRD100
        private static async void OnApplicationStarted(string internalOpenIdUrl)
#pragma warning restore VSTHRD100
        {
            try
            {
                using var httpClient = new HttpClient();

                var uriOpenId = new Uri(internalOpenIdUrl);
                var uriKeyGet = new Uri($"https://{uriOpenId.Authority}/token/cache");

                await httpClient.GetAsync(uriKeyGet).ConfigureAwait(false);
            }
#pragma warning disable CA1031
            catch
#pragma warning restore CA1031
            {
                // ignored
            }
        }
    }
}
