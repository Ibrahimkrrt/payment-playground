using PaymentGateway.Models.Records;

namespace PaymentGateway.Services.Validation;

public static class PaymentRequestValidator
{
    public static IReadOnlyList<string> Validate(
        PostPaymentRequest request,
        IReadOnlyList<string> supportedCurrencies,
        DateTimeOffset now)
    {
        var errors = new List<string>();

        if (!IsDigits(request.CardNumber, minLength: 14, maxLength: 19))
            errors.Add("card_number must be 14-19 digits.");

        if (!IsValidMonth(request.ExpiryMonth))
            errors.Add("expiry_month must be between 1 and 12.");

        if (request.ExpiryYear is null)
            errors.Add("expiry_year is required.");

        if (IsExpired(request.ExpiryMonth, request.ExpiryYear, now))
            errors.Add("expiry_month and expiry_year must be in the future.");

        if (request.Currency is null || !supportedCurrencies.Contains(request.Currency))
            errors.Add($"currency must be one of {string.Join(", ", supportedCurrencies)}.");

        if (request.Amount is null or <= 0)
            errors.Add("amount must be a positive integer in minor units.");

        if (!IsDigits(request.Cvv, minLength: 3, maxLength: 4))
            errors.Add("cvv must be 3-4 digits.");

        return errors;
    }

    private static bool IsValidMonth(int? month) => month is >= 1 and <= 12;

    private static bool IsExpired(int? month, int? year, DateTimeOffset now)
    {
        if (!IsValidMonth(month) || year is null)
            return false;

        return year < now.Year || (year == now.Year && month <= now.Month);
    }

    private static bool IsDigits(string? value, int minLength, int maxLength) =>
        value is not null
        && value.Length >= minLength
        && value.Length <= maxLength
        && value.All(char.IsAsciiDigit);
}
