using PaymentGateway.Models.Records;
using PaymentGateway.Models.Results;

namespace PaymentGateway.Models.Interfaces.Services;

public interface IBankClient
{
    Task<AuthorizationResult> AuthorizeAsync(BankPaymentRequest request, CancellationToken ct = default);
}
