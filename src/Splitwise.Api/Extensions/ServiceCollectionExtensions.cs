using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi.Models;
using Splitwise.Core.Interfaces;
using Splitwise.Core.Services;
using Splitwise.Core.Validators;
using Splitwise.Infrastructure;

namespace Splitwise.Api.Extensions;

public static class CorsPolicies
{
    public const string Frontend = "Frontend";
}

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IGroupService, GroupService>();
        services.AddScoped<IExpenseService, ExpenseService>();
        services.AddScoped<IBalanceService, BalanceService>();
        services.AddScoped<IUserService, UserService>();

        // Усі валідатори зі збірки Core: новий DTO отримує новий валідатор — і він підхоплюється автоматично
        services.AddValidatorsFromAssemblyContaining<CreateGroupDtoValidator>();

        return services;
    }

    public static IServiceCollection AddIdentityAuth(this IServiceCollection services)
    {
        // Єдина схема — Bearer: запит без токена дає 401, а не редірект на сторінку логіну
        services.AddAuthentication().AddBearerToken(IdentityConstants.BearerScheme);
        services.AddAuthorization();

        services.AddIdentityCore<ApplicationUser>(options => options.User.RequireUniqueEmail = true)
            .AddEntityFrameworkStores<AppDbContext>()
            .AddApiEndpoints();

        // Підтвердження пошти не використовується, тому листи нікуди не надсилаємо
        services.AddSingleton<IEmailSender<ApplicationUser>, NoOpEmailSender>();

        return services;
    }

    public static IServiceCollection AddFrontendCors(this IServiceCollection services, IConfiguration configuration)
    {
        // Дозволені origin беремо з конфігурації; у продакшні це змінні середовища Cors__AllowedOrigins__0, ...
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        services.AddCors(options => options.AddPolicy(CorsPolicies.Frontend, policy =>
            policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));

        return services;
    }

    public static IServiceCollection AddSwaggerDocs(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "Splitwise API", Version = "v1" });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                In = ParameterLocation.Header,
                Description = "Вставте accessToken, який повернув POST /api/auth/login"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }
}

internal sealed class NoOpEmailSender : IEmailSender<ApplicationUser>
{
    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) => Task.CompletedTask;
    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) => Task.CompletedTask;
    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) => Task.CompletedTask;
}
