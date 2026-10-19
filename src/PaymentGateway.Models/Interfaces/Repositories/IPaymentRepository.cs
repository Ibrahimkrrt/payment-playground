using PaymentGateway.Models.Domain;
using PaymentGateway.Models.Results;

namespace PaymentGateway.Models.Interfaces.Repositories;

public interface IPaymentRepository
{
    Task<FindResult<Payment>> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <exception cref="InvalidOperationException">A payment with the same id already exists.</exception>
    Task CreateAsync(Payment payment, CancellationToken ct = default);
}
