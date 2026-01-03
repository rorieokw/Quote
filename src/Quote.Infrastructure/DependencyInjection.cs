using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quote.Application.Common.Interfaces;
using Quote.Infrastructure.Data;
using Quote.Infrastructure.Services;

namespace Quote.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<QuoteDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<QuoteDbContext>());

        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Google Maps Service
        services.AddMemoryCache();
        services.AddHttpClient<IGoogleMapsService, GoogleMapsService>();

        // Stripe Payment Service
        services.AddScoped<Application.Payments.Services.IStripeService, StripeService>();

        // Blob Storage Service (local file storage)
        services.AddScoped<IBlobStorageService, LocalBlobStorageService>();

        services.AddHttpContextAccessor();

        return services;
    }
}
