using PaymentGateway.Models.Interfaces.Repositories;

namespace PaymentGateway.Repository.Repositories;

public sealed class InMemoryCurrencyRepository : ICurrencyRepository
{
    private static readonly IReadOnlyList<string> SupportedCurrencies = ["GBP", "USD", "EUR"];

    public Task<IReadOnlyList<string>> GetSupportedAsync(CancellationToken ct = default) =>
        Task.FromResult(SupportedCurrencies);
}
