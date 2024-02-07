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
using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Secrets;
using Energinet.DataHub.Core.App.Common.Diagnostics.HealthChecks;
using Energinet.DataHub.Core.App.WebApp.Authentication;
using Energinet.DataHub.Core.App.WebApp.Authorization;
using Energinet.DataHub.Core.App.WebApp.Diagnostics.HealthChecks;
using Energinet.DataHub.Core.Logging.LoggingScopeMiddleware;
using Energinet.DataHub.MarketParticipant.Application.Security;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Common.Configuration;
using Energinet.DataHub.MarketParticipant.Common.Extensions;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Extensions;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Security;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi
{
    public class Startup : Common.StartupBase
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
            app.UseCommonExceptionHandling(builder =>
            {
                const string prefix = "market_participant";
                builder.Use(new FluentValidationExceptionHandler(prefix));
                builder.Use(new NotFoundValidationExceptionHandler(prefix));
                builder.Use(new DataValidationExceptionHandler(prefix));
                builder.Use(new FallbackExceptionHandler(prefix));
            });

            app.UseLoggingScope();
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
        }

        public void ConfigureServices(IServiceCollection services)
        {
            Initialize(_configuration, services);
        }

        protected override void Configure(IConfiguration configuration, IServiceCollection services)
        {
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services
                .AddControllers()
                .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

            services.AddSingleton<IExternalTokenValidator>(_ =>
            {
                var externalOpenIdUrl = configuration.GetSetting(Settings.ExternalOpenIdUrl);
                var backendAppId = configuration.GetSetting(Settings.BackendBffAppId);
                return new ExternalTokenValidator(externalOpenIdUrl, backendAppId);
            });

            services.AddSingleton<ISigningKeyRing>(_ =>
            {
                var tokenKeyVaultUri = configuration.GetSetting(Settings.TokenKeyVault);
                var tokenKeyName = configuration.GetSetting(Settings.TokenKeyName);

                var tokenCredentials = new DefaultAzureCredential();

                var keyClient = new KeyClient(tokenKeyVaultUri, tokenCredentials);
                return new SigningKeyRing(Clock.Instance, keyClient, tokenKeyName);
            });

            services.AddSingleton<SecretClient>(_ =>
            {
                var certificateKeyVaultUri = configuration.GetSetting(Settings.CertificateKeyVault);
                var defaultCredentials = new DefaultAzureCredential();
                return new SecretClient(certificateKeyVaultUri, defaultCredentials);
            });

            services.AddSingleton<ICertificateService>(s =>
            {
                var certificateClient = s.GetRequiredService<SecretClient>();
                var logger = s.GetRequiredService<ILogger<CertificateService>>();
                var certificateValidation = s.GetRequiredService<ICertificateValidation>();
                return new CertificateService(certificateClient, certificateValidation, logger);
            });

            SetupAuthentication(configuration, services);

            services.AddPermissionAuthorization();
            services.AddUserAuthentication<FrontendUser, FrontendUserProvider>();
            services.AddScoped<IAuditIdentityProvider, FrontendUserAuditIdentityProvider>();

            // Health check
            services
                .AddHealthChecks()
                .AddLiveCheck()
                .AddDbContextCheck<MarketParticipantDbContext>()
                .AddCheck<GraphApiHealthCheck>("Graph API Access")
                .AddCheck<SigningKeyRingHealthCheck>("Signing Key Access")
                .AddCheck<CertificateKeyVaultHealthCheck>("Certificate Key Vault Access");

            services.AddHttpLoggingScope("mark-part");
            services.AddSwaggerGen(c =>
            {
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
                c.SupportNonNullableReferenceTypes();
                c.UseAllOfToExtendReferenceSchemas();

                var securityRequirement = new OpenApiSecurityRequirement { { securitySchema, new[] { "Bearer" } } };

                c.AddSecurityRequirement(securityRequirement);
            });
        }

        protected virtual void SetupAuthentication(IConfiguration configuration, IServiceCollection services)
        {
            var externalOpenIdUrl = configuration.GetSetting(Settings.ExternalOpenIdUrl);
            var internalOpenIdUrl = configuration.GetSetting(Settings.InternalOpenIdUrl);
            var backendAppId = configuration.GetSetting(Settings.BackendBffAppId);
            services.AddJwtBearerAuthentication(externalOpenIdUrl, internalOpenIdUrl, backendAppId);
        }
    }
}
