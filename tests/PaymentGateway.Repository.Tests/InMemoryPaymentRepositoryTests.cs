using PaymentGateway.Models.Domain;
using PaymentGateway.Models.Results;
using PaymentGateway.Repository.Repositories;

namespace PaymentGateway.Repository.Tests;

public class InMemoryPaymentRepositoryTests
{
    private readonly InMemoryPaymentRepository _repository = new();

    private static Payment NewPayment() =>
        new(Guid.NewGuid(), PaymentStatus.Authorized, "8877", 4, 2027, "GBP", 100);

    [Fact]
    public async Task Created_payment_can_be_read_back()
    {
        var payment = NewPayment();

        await _repository.CreateAsync(payment);
        var result = await _repository.GetByIdAsync(payment.Id);

        var found = Assert.IsType<FindResult<Payment>.Found>(result);
        Assert.Equal(payment, found.Value);
    }

    [Fact]
    public async Task Unknown_id_returns_not_found()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        Assert.IsType<FindResult<Payment>.NotFound>(result);
    }

    [Fact]
    public async Task Duplicate_id_throws_and_keeps_original()
    {
        var original = NewPayment();
        await _repository.CreateAsync(original);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _repository.CreateAsync(original with { Status = PaymentStatus.Declined }));

        var stored = Assert.IsType<FindResult<Payment>.Found>(await _repository.GetByIdAsync(original.Id));
        Assert.Equal(original, stored.Value);
    }
}
