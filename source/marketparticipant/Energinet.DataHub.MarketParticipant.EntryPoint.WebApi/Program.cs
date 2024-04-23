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

using System.Reflection;
using System.Text.Json.Serialization;
using Energinet.DataHub.Core.App.WebApp.Extensions.Builder;
using Energinet.DataHub.Core.App.WebApp.Extensions.DependencyInjection;
using Energinet.DataHub.Core.Logging.LoggingScopeMiddleware;
using Energinet.DataHub.MarketParticipant.Application.Security;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

const string subsystemName = "mark-part";

var startup = new Startup();
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpLoggingScope(subsystemName);
builder.Services.AddApplicationInsightsForWebApp(subsystemName);
builder.Services.AddHealthChecksForWebApp();

builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services
    .AddSwaggerForWebApp(Assembly.GetExecutingAssembly(), subsystemName);

builder.Services
    .AddUserAuthenticationForWebApp<FrontendUser, FrontendUserProvider>()
    .AddPermissionAuthorizationForWebApp();

startup.Initialize(builder.Configuration, builder.Services);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseRouting();
app.UseSwaggerForWebApp();
app.UseHttpsRedirection();
app.UseCommonExceptionHandling(exceptionBuilder =>
{
    exceptionBuilder.Use(new FluentValidationExceptionHandler(subsystemName));
    exceptionBuilder.Use(new NotFoundValidationExceptionHandler(subsystemName));
    exceptionBuilder.Use(new DataValidationExceptionHandler(subsystemName));
    exceptionBuilder.Use(new FallbackExceptionHandler(subsystemName));
});

app.UseLoggingScope();
app.UseAuthentication();
app.UseAuthorization();
app.UseUserMiddlewareForWebApp<FrontendUser>();
app.MapControllers().RequireAuthorization();

app.MapLiveHealthChecks();
app.MapReadyHealthChecks();

app.Run();
