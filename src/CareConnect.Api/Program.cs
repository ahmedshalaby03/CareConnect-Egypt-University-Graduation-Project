using CareConnect.Api.Authorization;
using CareConnect.Api.Extensions;
using CareConnect.Api.Filters;
using CareConnect.Api.Middleware;
using CareConnect.Api.Services;
using CareConnect.Application;
using CareConnect.Application.Interfaces;
using CareConnect.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog is configured from the "Serilog" section so log levels and sinks can be changed
// without a rebuild.
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization(options => options.AddCareConnectPolicies());
builder.Services.AddCareConnectCors(builder.Configuration);

builder.Services.AddControllers(options => options.Filters.Add<FluentValidationFilter>());

// The FluentValidation filter owns validation responses, so the framework's automatic
// 400 is turned off to keep a single response shape.
builder.Services.Configure<ApiBehaviorOptions>(options =>
    options.SuppressModelStateInvalidFilter = true);

builder.Services.AddSwaggerWithJwt();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "CareConnect Egypt API v1");
        options.DocumentTitle = "CareConnect Egypt API";
    });
}
else
{
    app.UseHttpsRedirection();
}

app.UseCors(CorsExtensions.PolicyName);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await app.SeedDatabaseAsync();
await app.SeedDemoDataAsync();

app.Run();

/// <summary>Exposed so the integration test project can host the API in memory.</summary>
public partial class Program;
