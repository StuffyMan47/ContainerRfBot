using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ContainerRfBot.Core.Interfaces;
using ContainerRfBot.Core.Interfaces.Settings;
using ContainerRfBot.Core.UseCases;
using ContainerRfBot.Infrastructure.Clients;
using ContainerRfBot.Infrastructure.DAL;
using ContainerRfBot.Bot.Services;
using ContainerRfBot.Infrastructure.DAL.Repositories;
using ContainerRfBot.Infrastructure.Services;
using Microsoft.OpenApi.Models;

namespace ContainerRfBot.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        
        services.AddScoped<INotificationService, MaxNotificationService>();
        services.AddHttpClient<IYooKassaService, YooKassaService>();
        
        services.AddHostedService<SubscriptionCheckJob>();
        
        services.AddSingleton<ISetting, Setting>();
        
        return services;
    }

    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.AddScoped<ManageSubscriptionUseCase>();
        return services;
    }
    
    public static IServiceCollection AddSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "ContainerRfBot API", Version = "v1" });
        });
        return services;
    }
}