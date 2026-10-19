namespace PaymentGateway.Models.Records;

public sealed record PostPaymentRequest(
    string? CardNumber,
    int? ExpiryMonth,
    int? ExpiryYear,
    string? Currency,
    int? Amount,
    string? Cvv)
{
    public override string ToString() =>
        $"PostPaymentRequest {{ ExpiryMonth = {ExpiryMonth}, ExpiryYear = {ExpiryYear}, Currency = {Currency}, Amount = {Amount} }}";
}
