using System;
using System.Collections.Generic;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.OpenApi.Models;

using k8s;

using KubeStatus;
using KubeStatus.Data;

using Prometheus;

Helper.BuildPodStatusDictionary();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
    .AddMicrosoftIdentityWebApp(builder.Configuration, "AzureAd", OpenIdConnectDefaults.AuthenticationScheme);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration, "AzureAd");

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireEditorRole",
        policy => policy.RequireRole("App.Edit", "App.Admin"));

    options.AddPolicy("RequireAdminRole",
        policy => policy.RequireRole("App.Admin"));

    options.FallbackPolicy = options.DefaultPolicy;
});

builder.Services.AddRazorPages()
    .AddMvcOptions(options => { })
    .AddMicrosoftIdentityUI();
builder.Services.AddServerSideBlazor()
    .AddMicrosoftIdentityConsentHandler();

builder.Services.AddSingleton<IKubernetes>(_ => Helper.GetKubernetesClient());
builder.Services.AddSingleton<SparkApplicationService>();
builder.Services.AddSingleton<KafkaConnectorService>();
builder.Services.AddSingleton<NamespaceService>();
builder.Services.AddSingleton<PodService>();
builder.Services.AddSingleton<DeploymentService>();
builder.Services.AddSingleton<StatefulSetService>();
builder.Services.AddSingleton<LogService>();
builder.Services.AddSingleton<HelmService>();

builder.Services.AddHealthChecks();
builder.Services.AddControllers();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "KubeStatus", Version = "v1" });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Auth",
        Type = SecuritySchemeType.OAuth2,
        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        },
        Flows = new OpenApiOAuthFlows
        {
            Implicit = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri($"https://login.microsoftonline.com/{builder.Configuration.GetSection("AzureAd").GetSection("TenantId").Value}/oauth2/v2.0/authorize"),
                TokenUrl = new Uri($"https://login.microsoftonline.com/{builder.Configuration.GetSection("AzureAd").GetSection("TenantId").Value}/oauth2/v2.0/token"),
                Scopes = new Dictionary<string, string>
                {
                    { $"{builder.Configuration.GetSection("AzureAd").GetSection("ClientId").Value}/.default", "" }
                },
            }
        }
    };
    c.AddSecurityDefinition(securityScheme.Reference.Id, securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {securityScheme, Array.Empty<string>()}
    });
});


var app = builder.Build();

var allowedHosts = builder.Configuration.GetSection("AllowedHosts").Value?.Split(',') ?? Array.Empty<string>();
if (allowedHosts.Length > 0)
{
    app.UseCors(x => x
    .WithOrigins(allowedHosts)
    .AllowAnyMethod()
    );
}

if (Helper.EnableSwagger() && !app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "KubeStatus v1");
    });
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "KubeStatus v1");
        c.OAuthClientId(builder.Configuration.GetSection("AzureAd").GetSection("ClientId").Value);
    });

    app.UseDeveloperExceptionPage();
    Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;
}
else
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();

app.UseRouting();

app.UseHttpMetrics();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.MapHealthChecks("/health");
app.MapMetrics("/metrics");

app.Run();
