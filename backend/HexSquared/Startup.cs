using Application;
using Application.IRepositories;
using Application.Services.Implementations;
using Application.Services.Interfaces;
using HexSquared.Configuration;
using Infrastructure.Repositories;
using Serilog;

namespace HexSquared;

public class Startup(IConfiguration configuration,  IHostEnvironment environment)
{
    // This method gets called by the runtime. Use it to add services to the DI container.
    public void ConfigureServices(IServiceCollection services)
    {

        if (environment.IsProduction())
        {
            services.AddSingleton<IHexConfiguration, ProductionHexConfiguration>();
        }
        else
        {
            services.AddSingleton<IHexConfiguration, LocalHexConfiguration>();
        }
        
        services.AddHostedService<AiPlayerService>();
        services.AddHostedService<GameCleanupService>();
        services.AddControllers();
        services.AddHealthChecks();
        services.AddSwaggerGen();

        services.AddSingleton<IGameRepository, GameRepository>();
        services.AddScoped<IGameService, GameService>();
    }


    // This method gets called by the runtime. Use it to configure eg the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        Log.Debug("IsDevelopment: {IsDevelopment}", env.IsDevelopment());

        if (env.IsDevelopment())
        {
            Log.Debug("Using UseDeveloperExceptionPage");
            app.UseDeveloperExceptionPage();

            Log.Debug("Setting cors => allow *");

            app.UseCors(builder =>
            {
                builder.WithOrigins(["http://localhost:3100", "http://localhost:5173", "hex.mazi.fun"]);
                builder.AllowCredentials();
                builder.AllowAnyHeader();
                builder.AllowAnyMethod();
            });
        }

        Log.Debug("Setting UseSwagger");
        app.UseSwagger();

        Log.Debug("Setting UseSwaggerUI");
        app.UseSwaggerUI();

        Log.Debug("Setting UseHttpsRedirection");
        app.UseHttpsRedirection();

        Log.Debug("Setting UseRouting");
        app.UseRouting();
        
        Log.Debug("Setting UseEndpoints");
        app.UseEndpoints(endpoints =>
        {
            Log.Debug("Setting endpoints => MapControllers");
            endpoints.MapControllers();

            Log.Debug("Setting endpoints => add health check");
            endpoints.MapHealthChecks("/health");
        });
    }
}
