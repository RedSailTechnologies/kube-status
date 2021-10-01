using System;
using KubeStatus.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Prometheus;

namespace KubeStatus
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddHealthChecks();
            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "KubeStatus", Version = "v1" });
            });
            services.AddSingleton<KafkaConnectorService>();
            services.AddSingleton<NamespaceService>();
            services.AddSingleton<PodService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            var allowedHosts = Configuration.GetSection("AllowedHosts").Value?.Split(',') ?? Array.Empty<string>();
            if (allowedHosts.Length > 0)
            {
                app.UseCors(x => x
                .WithOrigins(allowedHosts)
                .AllowAnyMethod()
                .WithHeaders("authorization")
                );
            }

            if (env.IsDevelopment() || Helper.EnableSwagger())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "KubeStatus v1"));
            }

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseStaticFiles();

            app.UseAuthentication();

            app.UseHttpMetrics();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
                endpoints.MapHealthChecks("/health");
                endpoints.MapMetrics("/metrics");
                endpoints.MapControllers();
            });
        }
    }
}
