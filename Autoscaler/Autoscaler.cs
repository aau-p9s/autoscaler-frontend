using Autoscaler.Lib.Kubernetes;
using Microsoft.OpenApi.Models;

namespace Autoscaler;

class Autoscaler {
    public static ArgumentParser Args = new();
    private static Database? _Database;
    
    readonly Thread thread;
    public Autoscaler() {
        thread = new(Scale);
        thread.Start();
    }

    public static Database Database() {
        _Database ??= new(Args.Get("--database"));
        return _Database;
    }

    public static void Main(string[] args) {
        Autoscaler autoscaler = new();
        Args.SetArgs(args);
        Database();
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers();

        // Add Swagger services
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1",
                Title = "AutoScaler API",
                Description = "API documentation for AutoScaler Frontend",
            });
        });

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowSpecificOrigin", builder =>
            {
                builder.WithOrigins("http://localhost:44411", "https://localhost:44411")
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        builder.WebHost.UseUrls("http://0.0.0.0:8080");

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
        }
        else
        {
            // Enable Swagger in development environment
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "AutoScaler API v1");
                options.RoutePrefix = string.Empty; // Makes Swagger UI available at the root ("/")
            });
        }

        app.UseStaticFiles();
        app.UseRouting();

        app.UseCors();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers().RequireCors("AllowSpecificOrigin");
        });
        app.Lifetime.ApplicationStopping.Register(() => {
        });

        app.Run();

    }
    
    public async void Scale() {
        Prometheus prometheus = new();
        Kubernetes kubernetes = new();
        while(true) {
            var data = await prometheus.QueryRange("sum(rate(container_cpu_usage_seconds_total{container=~\"stregsystemet\"}[5m]))/4*100", DateTime.Now.AddDays(-7), DateTime.Now);
            Database().InsertHistorical(data);
            Database().Clean();
            // TODO: get ML results here, instead of hardcoding it
            var replicas = 2;
            // scale cluster
            // get replicaset name
            Dictionary<string, Dictionary<string, int>> patchData = new() {{
                "spec", new() {{
                    "replicas",replicas
                }}
            }};
            kubernetes.Patch($"/apis/apps/v1/namespaces/default/deployments/{Args.Get("--deployment")}/scale", patchData);
            
            Thread.Sleep(int.Parse(Args.Get("--period")));
        }
    }
}