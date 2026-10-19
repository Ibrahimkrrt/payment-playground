using Microsoft.Extensions.DependencyInjection;
using PaymentGateway.Models.Interfaces.Repositories;
using PaymentGateway.Repository.Repositories;

namespace PaymentGateway.Repository;

public static class DependencyInjection
{
    public static IServiceCollection AddRepository(this IServiceCollection services)
    {
        services.AddSingleton<IPaymentRepository, InMemoryPaymentRepository>();
        services.AddSingleton<ICurrencyRepository, InMemoryCurrencyRepository>();
        return services;
    }
}
