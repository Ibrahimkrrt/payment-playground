using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PaymentGateway.Models.Interfaces.Services;
using PaymentGateway.Services.Clients;
using PaymentGateway.Services.Services;

namespace PaymentGateway.Services;

public static class DependencyInjection
{
    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddBankClient(configuration);

        return services;
    }

    private static void AddBankClient(this IServiceCollection services, IConfiguration configuration)
    {
        var baseUrl = configuration["Bank:BaseUrl"]
            ?? throw new InvalidOperationException("Bank:BaseUrl is not configured.");

        services.AddHttpClient<IBankClient, BankClient>(client =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(10);
        });
    }
}
