namespace PaymentGateway.Models.Interfaces.Repositories;

public interface ICurrencyRepository
{
    /// <returns>ISO 4217 codes (upper case) the gateway accepts.</returns>
    Task<IReadOnlyList<string>> GetSupportedAsync(CancellationToken ct = default);
}
