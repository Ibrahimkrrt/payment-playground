namespace PaymentGateway.Models.Records;

public sealed record BankPaymentRequest(
    string CardNumber,
    string ExpiryDate,
    string Currency,
    int Amount,
    string Cvv)
{
    public override string ToString() =>
        $"BankPaymentRequest {{ ExpiryDate = {ExpiryDate}, Currency = {Currency}, Amount = {Amount} }}";
}
