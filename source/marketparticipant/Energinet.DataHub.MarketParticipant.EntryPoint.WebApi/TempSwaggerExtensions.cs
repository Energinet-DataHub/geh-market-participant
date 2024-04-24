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

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi;

public static class TempSwaggerExtensions
{
    /// <summary>
    /// Register middleware for enabling an ASP.NET Core app
    /// to generate Open API specifications and work with Swagger UI.
    /// </summary>
    public static IApplicationBuilder UseTempSwaggerForWebApp(this IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "Energinet.DataHub.MarketParticipant.EntryPoint.WebApi v1"));
        return app;
    }

    public static IServiceCollection AddTempSwaggerForWebApi(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc(
                "v1",
                new OpenApiInfo
                {
                    Title = "Energinet.DataHub.MarketParticipant.EntryPoint.WebApi",
                    Version = "v1",
                });
            var securitySchema = new OpenApiSecurityScheme
            {
                Description =
                    "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer",
                }
            };
            c.AddSecurityDefinition("Bearer", securitySchema);
            c.SupportNonNullableReferenceTypes();
            c.UseAllOfToExtendReferenceSchemas();
            var securityRequirement = new OpenApiSecurityRequirement
            {
                {
                    securitySchema, new[]
                    {
                        "Bearer",
                    }
                },
            };
            c.AddSecurityRequirement(securityRequirement);
        });

        return services;
    }
}
