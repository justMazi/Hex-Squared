using Application;
using Application.IRepositories;
using Application.Services.Implementations;
using Application.Services.Interfaces;
using HexSquared.Configuration;
using Infrastructure.Repositories;
using Serilog;

namespace HexSquared;

public class Startup(IConfiguration configuration)
{
    private readonly IConfiguration _configuration = configuration;

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services, IHostEnvironment environment)
    {

        if (environment.IsProduction())
        {
            services.AddSingleton<IHexConfiguration, ProductionHexConfiguration>()
        }
        else
        {
            services.AddSingleton<IHexConfiguration, LocalHexConfiguration>();
        }
        
        services.AddHostedService<AiPlayerService>();
        services.AddControllers();
        services.AddHealthChecks();
        services.AddSwaggerGen();

        services.AddSingleton<IGameRepository, GameRepository>();
        services.AddScoped<IGameService, GameService>();
    }


    // This method gets called by the runtime. Use this method to configure eg the HTTP request pipeline.
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
                builder.AllowCredentials();
                builder.SetIsOriginAllowed(_ => true);
                builder.AllowAnyHeader();
                builder.AllowAnyMethod();
            });
        }

        /*
        Log.Debug("Setting Exception handling middleware");
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        */

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
