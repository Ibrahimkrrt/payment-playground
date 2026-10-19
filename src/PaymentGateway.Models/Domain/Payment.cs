namespace PaymentGateway.Models.Domain;

public enum PaymentStatus
{
    Authorized,
    Declined,
    Rejected
}

public sealed record Payment(
    Guid Id,
    PaymentStatus Status,
    string CardNumberLastFour,
    int ExpiryMonth,
    int ExpiryYear,
    string Currency,
    int Amount);
