using PaymentGateway.Repository.Repositories;

namespace PaymentGateway.Repository.Tests;

public class InMemoryCurrencyRepositoryTests
{
    [Fact]
    public async Task Returns_GBP_USD_EUR()
    {
        var currencies = await new InMemoryCurrencyRepository().GetSupportedAsync();

        Assert.Equal(["GBP", "USD", "EUR"], currencies);
    }
}
