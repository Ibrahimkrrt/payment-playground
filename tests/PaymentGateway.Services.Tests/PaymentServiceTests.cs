using Microsoft.Extensions.Logging.Abstractions;
using PaymentGateway.Models.Domain;
using PaymentGateway.Models.Records;
using PaymentGateway.Models.Results;
using PaymentGateway.Services.Services;

namespace PaymentGateway.Services.Tests;

public class PaymentServiceTests
{
    private readonly FakeBankClient _bank = new();
    private readonly FakePaymentRepository _repository = new();
    private readonly PaymentService _service;

    public PaymentServiceTests() =>
        _service = CreateService(new FakeCurrencyRepository(TestData.SupportedCurrencies));

    private PaymentService CreateService(FakeCurrencyRepository currencies) =>
        new(_bank, _repository, currencies, new FixedTimeProvider(TestData.Now), NullLogger<PaymentService>.Instance);

    [Fact]
    public async Task Authorized_payment_is_stored_and_returned()
    {
        var result = await _service.ProcessAsync(TestData.ValidRequest(TestData.AuthorizedCard));

        var payment = Assert.IsType<ProcessPaymentResult.Processed>(result).Payment;
        Assert.NotEqual(Guid.Empty, payment.Id);
        Assert.Equal(PaymentStatus.Authorized, payment.Status);
        Assert.Equal("8877", payment.CardNumberLastFour);
        Assert.Equal(4, payment.ExpiryMonth);
        Assert.Equal(2027, payment.ExpiryYear);
        Assert.Equal("GBP", payment.Currency);
        Assert.Equal(100, payment.Amount);
        Assert.Equal(payment, PaymentResponse.FromPayment(_repository.Payments[payment.Id]));
    }

    [Fact]
    public async Task Declined_payment_is_stored_and_returned()
    {
        var result = await _service.ProcessAsync(TestData.ValidRequest(TestData.DeclinedCard));

        var payment = Assert.IsType<ProcessPaymentResult.Processed>(result).Payment;
        Assert.Equal(PaymentStatus.Declined, payment.Status);
        Assert.Equal(PaymentStatus.Declined, _repository.Payments[payment.Id].Status);
    }

    [Fact]
    public async Task Bank_receives_the_card_details_with_a_padded_expiry_date()
    {
        await _service.ProcessAsync(TestData.ValidRequest(TestData.AuthorizedCard));

        var sent = Assert.Single(_bank.Requests);
        Assert.Equal(TestData.AuthorizedCard, sent.CardNumber);
        Assert.Equal("04/2027", sent.ExpiryDate);
        Assert.Equal("GBP", sent.Currency);
        Assert.Equal(100, sent.Amount);
        Assert.Equal("123", sent.Cvv);
    }

    [Fact]
    public async Task Invalid_request_is_rejected_without_calling_the_bank()
    {
        var result = await _service.ProcessAsync(TestData.ValidRequest() with { Cvv = "1" });

        var rejected = Assert.IsType<ProcessPaymentResult.Rejected>(result);
        Assert.Equal(["cvv must be 3-4 digits."], rejected.Errors);
        Assert.Empty(_bank.Requests);
        Assert.Empty(_repository.Payments);
    }

    [Fact]
    public async Task Supported_currencies_come_from_the_repository()
    {
        var service = CreateService(new FakeCurrencyRepository("JPY"));

        var yen = await service.ProcessAsync(TestData.ValidRequest() with { Currency = "JPY" });
        var pounds = await service.ProcessAsync(TestData.ValidRequest() with { Currency = "GBP" });

        Assert.IsType<ProcessPaymentResult.Processed>(yen);
        var rejected = Assert.IsType<ProcessPaymentResult.Rejected>(pounds);
        Assert.Equal(["currency must be one of JPY."], rejected.Errors);
    }

    [Fact]
    public async Task Bank_unavailable_stores_nothing()
    {
        var result = await _service.ProcessAsync(TestData.ValidRequest(TestData.BankUnavailableCard));

        Assert.IsType<ProcessPaymentResult.BankUnavailable>(result);
        Assert.Single(_bank.Requests);
        Assert.Empty(_repository.Payments);
    }

    [Fact]
    public async Task GetById_returns_the_stored_payment()
    {
        var processed = await _service.ProcessAsync(TestData.ValidRequest());
        var created = Assert.IsType<ProcessPaymentResult.Processed>(processed).Payment;

        var result = await _service.GetByIdAsync(created.Id);

        var found = Assert.IsType<FindResult<PaymentResponse>.Found>(result);
        Assert.Equal(created, found.Value);
    }

    [Fact]
    public async Task GetById_with_unknown_id_returns_not_found()
    {
        var result = await _service.GetByIdAsync(Guid.NewGuid());

        Assert.IsType<FindResult<PaymentResponse>.NotFound>(result);
    }
}
