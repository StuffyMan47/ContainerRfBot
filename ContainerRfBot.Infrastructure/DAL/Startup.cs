using ContainerRfBot.Infrastructure.DAL.DbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ContainerRfBot.Infrastructure.DAL;

public static class Startup
{
    public static IServiceCollection AddDataAccessLayer(this IServiceCollection services, IConfiguration config)
    {
        services.AddAppDbContext(config);

        return services;
    }
    
    private static IServiceCollection AddAppDbContext(this IServiceCollection services, IConfiguration config)
    {
        string? dbConnectionString = config.GetConnectionString("DBConnectionString");
        return services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(dbConnectionString, x => x
                .MigrationsAssembly(typeof(AppDbContext).Assembly.ToString())
                .MigrationsHistoryTable("__EFMigrationsHistory", "public")
            );
        });
    }
}