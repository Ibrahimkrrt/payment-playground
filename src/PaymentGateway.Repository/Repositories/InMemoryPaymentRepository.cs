using System.Collections.Concurrent;
using PaymentGateway.Models.Domain;
using PaymentGateway.Models.Interfaces.Repositories;
using PaymentGateway.Models.Results;

namespace PaymentGateway.Repository.Repositories;

public sealed class InMemoryPaymentRepository : IPaymentRepository
{
    private readonly ConcurrentDictionary<Guid, Payment> _payments = new();

    public Task<FindResult<Payment>> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult<FindResult<Payment>>(_payments.TryGetValue(id, out var payment)
            ? new FindResult<Payment>.Found(payment)
            : new FindResult<Payment>.NotFound());

    public Task CreateAsync(Payment payment, CancellationToken ct = default)
    {
        if (!_payments.TryAdd(payment.Id, payment))
            throw new InvalidOperationException($"Payment {payment.Id} already exists.");
        return Task.CompletedTask;
    }
}
