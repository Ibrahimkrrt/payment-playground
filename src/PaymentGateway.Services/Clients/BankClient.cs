using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using PaymentGateway.Models.Interfaces.Services;
using PaymentGateway.Models.Records;
using PaymentGateway.Models.Results;

namespace PaymentGateway.Services.Clients;

public sealed class BankClient(HttpClient httpClient, ILogger<BankClient> logger) : IBankClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public async Task<AuthorizationResult> AuthorizeAsync(BankPaymentRequest request, CancellationToken ct = default)
    {
        try
        {
            using var response = await httpClient.PostAsJsonAsync("payments", request, JsonOptions, ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Bank returned status {StatusCode}", (int)response.StatusCode);
                return new AuthorizationResult.Unavailable();
            }

            var reply = await response.Content.ReadFromJsonAsync<BankPaymentResponse>(JsonOptions, ct);
            if (reply is null)
                return new AuthorizationResult.Unavailable();

            return reply.Authorized
                ? new AuthorizationResult.Authorized()
                : new AuthorizationResult.Declined();
        }
        catch (Exception ex) when (IsBankFailure(ex, ct))
        {
            logger.LogWarning(ex, "Bank call failed");
            return new AuthorizationResult.Unavailable();
        }
    }

    // HttpClient timeouts throw TaskCanceledException without the caller's token being cancelled.
    private static bool IsBankFailure(Exception ex, CancellationToken ct) =>
        ex is HttpRequestException or JsonException
        || (ex is TaskCanceledException && !ct.IsCancellationRequested);
}
