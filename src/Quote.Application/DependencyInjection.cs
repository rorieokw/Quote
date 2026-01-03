using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Quote.Application.Common.Behaviours;
using Quote.Application.Invoicing.Services;
using Quote.Application.LeadScoring.Services;
using Quote.Application.PriceBenchmarking.Services;
using Quote.Application.RecurringJobs.Services;
using Quote.Application.Scheduling.Services;

namespace Quote.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddAutoMapper(cfg => cfg.AddMaps(Assembly.GetExecutingAssembly()));
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
        });

        // Register application services
        services.AddScoped<ILeadScoringService, LeadScoringService>();
        services.AddScoped<ISchedulingService, SchedulingService>();
        services.AddScoped<IInvoicingService, InvoicingService>();
        services.AddScoped<IRecurringJobService, RecurringJobService>();
        services.AddScoped<IPriceBenchmarkingService, PriceBenchmarkingService>();

        return services;
    }
}
