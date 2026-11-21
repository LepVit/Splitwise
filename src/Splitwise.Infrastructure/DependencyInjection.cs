using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Splitwise.Core.Interfaces;
using Splitwise.Infrastructure.Repositories;

namespace Splitwise.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            var raw = configuration.GetConnectionString("Default");
            if (string.IsNullOrWhiteSpace(raw))
                throw new InvalidOperationException(
                    "Connection string 'Default' is not configured. Set it with user-secrets locally " +
                    "or with the ConnectionStrings__Default environment variable on the server.");

            options.UseNpgsql(ConnectionStringHelper.Normalize(raw));
        });

        services.AddScoped<IGroupRepository, GroupRepository>();
        services.AddScoped<IExpenseRepository, ExpenseRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
