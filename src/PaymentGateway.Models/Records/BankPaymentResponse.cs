namespace PaymentGateway.Models.Records;

public sealed record BankPaymentResponse(bool Authorized, string? AuthorizationCode);
