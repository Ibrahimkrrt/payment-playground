using PaymentGateway.Models.Records;
using PaymentGateway.Services.Validation;

namespace PaymentGateway.Services.Tests;

public class PaymentRequestValidatorTests
{
    private static IReadOnlyList<string> Validate(PostPaymentRequest request) =>
        PaymentRequestValidator.Validate(request, TestData.SupportedCurrencies, TestData.Now);

    [Fact]
    public void Valid_request_has_no_errors() =>
        Assert.Empty(Validate(TestData.ValidRequest()));

    [Theory]
    [InlineData("12345678901234")]      // 14 digits, minimum
    [InlineData("1234567890123456789")] // 19 digits, maximum
    public void Card_number_length_boundaries_are_accepted(string cardNumber) =>
        Assert.Empty(Validate(TestData.ValidRequest() with { CardNumber = cardNumber }));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1234567890123")]        // 13 digits
    [InlineData("12345678901234567890")] // 20 digits
    [InlineData("2222 4053 4324 8877")]  // spaces
    [InlineData("222240534324887a")]     // letter
    [InlineData("٢٢٢٢٤٠٥٣٤٣٢٤٨٨٧٧")]     // non-ASCII (Arabic-Indic) digits
    public void Invalid_card_numbers_are_rejected(string? cardNumber) =>
        Assert.Equal(["card_number must be 14-19 digits."],
            Validate(TestData.ValidRequest() with { CardNumber = cardNumber }));

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(13)]
    [InlineData(-1)]
    public void Invalid_months_are_rejected(int? month) =>
        Assert.Equal(["expiry_month must be between 1 and 12."],
            Validate(TestData.ValidRequest() with { ExpiryMonth = month }));

    [Theory]
    [InlineData(1, 2027)]
    [InlineData(12, 2027)]
    [InlineData(7, 2025)]  // next month
    [InlineData(1, 10000)] // far future
    public void Future_expiry_is_accepted(int month, int year) =>
        Assert.Empty(Validate(TestData.ValidRequest() with { ExpiryMonth = month, ExpiryYear = year }));

    [Theory]
    [InlineData(6, 2025)]  // current month
    [InlineData(5, 2025)]  // last month
    [InlineData(12, 2024)] // last year
    [InlineData(1, 0)]     // nonsense year
    public void Current_or_past_expiry_is_rejected(int month, int year) =>
        Assert.Equal(["expiry_month and expiry_year must be in the future."],
            Validate(TestData.ValidRequest() with { ExpiryMonth = month, ExpiryYear = year }));

    [Fact]
    public void Missing_year_is_rejected() =>
        Assert.Equal(["expiry_year is required."],
            Validate(TestData.ValidRequest() with { ExpiryYear = null }));

    [Theory]
    [InlineData("GBP")]
    [InlineData("USD")]
    [InlineData("EUR")]
    public void Supported_currencies_are_accepted(string currency) =>
        Assert.Empty(Validate(TestData.ValidRequest() with { Currency = currency }));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("JPY")] // 3 chars but unsupported
    [InlineData("gbp")] // ISO codes are upper case
    [InlineData("GB")]
    [InlineData("GBPX")]
    public void Invalid_currencies_are_rejected(string? currency) =>
        Assert.Equal(["currency must be one of GBP, USD, EUR."],
            Validate(TestData.ValidRequest() with { Currency = currency }));

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-1)]
    public void Non_positive_amounts_are_rejected(int? amount) =>
        Assert.Equal(["amount must be a positive integer in minor units."],
            Validate(TestData.ValidRequest() with { Amount = amount }));

    [Fact]
    public void Amount_of_one_minor_unit_is_accepted() =>
        Assert.Empty(Validate(TestData.ValidRequest() with { Amount = 1 }));

    [Theory]
    [InlineData("123")]
    [InlineData("1234")]
    [InlineData("012")] // leading zero
    public void Valid_cvvs_are_accepted(string cvv) =>
        Assert.Empty(Validate(TestData.ValidRequest() with { Cvv = cvv }));

    [Theory]
    [InlineData(null)]
    [InlineData("12")]
    [InlineData("12345")]
    [InlineData("12a")]
    [InlineData(" 123")]
    public void Invalid_cvvs_are_rejected(string? cvv) =>
        Assert.Equal(["cvv must be 3-4 digits."],
            Validate(TestData.ValidRequest() with { Cvv = cvv }));

    [Fact]
    public void All_errors_are_reported_together()
    {
        var empty = new PostPaymentRequest(null, null, null, null, null, null);

        Assert.Equal(6, Validate(empty).Count);
    }
}
