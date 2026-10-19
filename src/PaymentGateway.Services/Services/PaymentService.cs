using Microsoft.Extensions.Logging;
using PaymentGateway.Models.Domain;
using PaymentGateway.Models.Interfaces.Repositories;
using PaymentGateway.Models.Interfaces.Services;
using PaymentGateway.Models.Records;
using PaymentGateway.Models.Results;
using PaymentGateway.Services.Validation;

namespace PaymentGateway.Services.Services;

public sealed class PaymentService(
    IBankClient bank,
    IPaymentRepository payments,
    ICurrencyRepository currencies,
    TimeProvider timeProvider) : IPaymentService
{
    private static readonly ProcessPaymentResult BankUnavailable = new ProcessPaymentResult.BankUnavailable();

    public async Task<ProcessPaymentResult> ProcessAsync(PostPaymentRequest request, CancellationToken ct = default)
    {
        var errors = await ValidateAsync(request, ct);
        if (errors.Count > 0)
            return new ProcessPaymentResult.Rejected(errors);

        var authorization = await bank.AuthorizeAsync(ToBankRequest(request), ct);

        return await authorization.Match(
            authorized: () => SaveAsync(request, PaymentStatus.Authorized, ct),
            declined: () => SaveAsync(request, PaymentStatus.Declined, ct),
            unavailable: () => Task.FromResult(BankUnavailable));
    }

    public async Task<FindResult<PaymentResponse>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var result = await payments.GetByIdAsync(id, ct);

        return result.Match<FindResult<PaymentResponse>>(
            found: payment => new FindResult<PaymentResponse>.Found(PaymentResponse.FromPayment(payment)),
            notFound: () => new FindResult<PaymentResponse>.NotFound());
    }

    private async Task<IReadOnlyList<string>> ValidateAsync(PostPaymentRequest request, CancellationToken ct)
    {
        var supportedCurrencies = await currencies.GetSupportedAsync(ct);
        return PaymentRequestValidator.Validate(request, supportedCurrencies, timeProvider.GetUtcNow());
    }

    private async Task<ProcessPaymentResult> SaveAsync(PostPaymentRequest request, PaymentStatus status, CancellationToken ct)
    {
        var payment = CreatePayment(request, status);
        await payments.CreateAsync(payment, ct);

        return new ProcessPaymentResult.Processed(PaymentResponse.FromPayment(payment));
    }

    private static BankPaymentRequest ToBankRequest(PostPaymentRequest request) => new(
        CardNumber: request.CardNumber!,
        ExpiryDate: $"{request.ExpiryMonth:D2}/{request.ExpiryYear}",
        Currency: request.Currency!,
        Amount: request.Amount!.Value,
        Cvv: request.Cvv!);

    private static Payment CreatePayment(PostPaymentRequest request, PaymentStatus status) => new(
        Id: Guid.NewGuid(),
        Status: status,
        CardNumberLastFour: request.CardNumber![^4..],
        ExpiryMonth: request.ExpiryMonth!.Value,
        ExpiryYear: request.ExpiryYear!.Value,
        Currency: request.Currency!,
        Amount: request.Amount!.Value);
}
