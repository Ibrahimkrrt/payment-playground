using PaymentGateway.Models.Domain;

namespace PaymentGateway.Models.Records;

public sealed record RejectedPaymentResponse(PaymentStatus Status, IReadOnlyList<string> Errors);
