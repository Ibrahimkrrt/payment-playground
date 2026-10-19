using PaymentGateway.Models.Domain;
using PaymentGateway.Models.Interfaces.Repositories;
using PaymentGateway.Models.Interfaces.Services;
using PaymentGateway.Models.Records;
using PaymentGateway.Models.Results;

namespace PaymentGateway.Services.Tests;

public sealed class FakeBankClient : IBankClient
{
    public List<BankPaymentRequest> Requests { get; } = [];

    public Task<AuthorizationResult> AuthorizeAsync(BankPaymentRequest request, CancellationToken ct = default)
    {
        Requests.Add(request);
        var lastDigit = request.CardNumber[^1] - '0';
        AuthorizationResult result = lastDigit switch
        {
            0 => new AuthorizationResult.Unavailable(),
            _ when lastDigit % 2 == 1 => new AuthorizationResult.Authorized(),
            _ => new AuthorizationResult.Declined()
        };
        return Task.FromResult(result);
    }
}

public sealed class FakePaymentRepository : IPaymentRepository
{
    public Dictionary<Guid, Payment> Payments { get; } = [];

    public Task<FindResult<Payment>> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult<FindResult<Payment>>(Payments.TryGetValue(id, out var payment)
            ? new FindResult<Payment>.Found(payment)
            : new FindResult<Payment>.NotFound());

    public Task CreateAsync(Payment payment, CancellationToken ct = default)
    {
        Payments.Add(payment.Id, payment);
        return Task.CompletedTask;
    }
}

public sealed class FakeCurrencyRepository(params string[] currencies) : ICurrencyRepository
{
    public Task<IReadOnlyList<string>> GetSupportedAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<string>>(currencies);
}

public sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}

public static class TestData
{
    public static readonly DateTimeOffset Now = new(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);

    public static readonly string[] SupportedCurrencies = ["GBP", "USD", "EUR"];

    // FakeBankClient decides by the last digit: odd is authorized, even is declined, 0 is unavailable.
    public const string AuthorizedCard = "2222405343248877";
    public const string DeclinedCard = "2222405343248112";
    public const string BankUnavailableCard = "2222405343248870";

    public static PostPaymentRequest ValidRequest(string cardNumber = AuthorizedCard) =>
        new(cardNumber, ExpiryMonth: 4, ExpiryYear: 2027, Currency: "GBP", Amount: 100, Cvv: "123");
}
