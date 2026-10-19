using PaymentGateway.Models.Domain;

namespace PaymentGateway.Models.Records;

public sealed record PaymentResponse(
    Guid Id,
    PaymentStatus Status,
    string CardNumberLastFour,
    int ExpiryMonth,
    int ExpiryYear,
    string Currency,
    int Amount)
{
    public static PaymentResponse FromPayment(Payment payment) => new(
        payment.Id,
        payment.Status,
        payment.CardNumberLastFour,
        payment.ExpiryMonth,
        payment.ExpiryYear,
        payment.Currency,
        payment.Amount);
}
