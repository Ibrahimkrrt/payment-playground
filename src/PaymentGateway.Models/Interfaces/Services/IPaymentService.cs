using PaymentGateway.Models.Records;
using PaymentGateway.Models.Results;

namespace PaymentGateway.Models.Interfaces.Services;

public interface IPaymentService
{
    Task<ProcessPaymentResult> ProcessAsync(PostPaymentRequest request, CancellationToken ct = default);
    Task<FindResult<PaymentResponse>> GetByIdAsync(Guid id, CancellationToken ct = default);
}
