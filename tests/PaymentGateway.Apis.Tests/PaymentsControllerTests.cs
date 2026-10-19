using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using PaymentGateway.Models.Interfaces.Services;
using PaymentGateway.Models.Records;
using PaymentGateway.Models.Results;

namespace PaymentGateway.Apis.Tests;

public sealed class PaymentsControllerTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    // FakeBankClient decides by the last digit: odd is authorized, even is declined, 0 is unavailable.
    private const string AuthorizedCard = "2222405343248877";
    private const string DeclinedCard = "2222405343248112";
    private const string BankUnavailableCard = "2222405343248870";

    private readonly FakeBankClient _bank = new();
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public PaymentsControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IBankClient>(_bank);
            services.AddSingleton<TimeProvider>(new FixedTimeProvider(new DateTimeOffset(2025, 6, 15, 0, 0, 0, TimeSpan.Zero)));
        }));
        _client = _factory.CreateClient();
    }

    public void Dispose() => _factory.Dispose();

    private static object Body(string cardNumber = AuthorizedCard, string cvv = "123", int expiryYear = 2027) => new
    {
        card_number = cardNumber,
        expiry_month = 4,
        expiry_year = expiryYear,
        currency = "GBP",
        amount = 100,
        cvv
    };

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();

    [Fact]
    public async Task Post_authorized_payment_returns_201_with_payment()
    {
        var response = await _client.PostAsJsonAsync("/api/payments", Body());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await ReadJson(response);
        Assert.Equal("Authorized", json.GetProperty("status").GetString());
        Assert.Equal("8877", json.GetProperty("card_number_last_four").GetString());
        Assert.Equal(4, json.GetProperty("expiry_month").GetInt32());
        Assert.Equal(2027, json.GetProperty("expiry_year").GetInt32());
        Assert.Equal("GBP", json.GetProperty("currency").GetString());
        Assert.Equal(100, json.GetProperty("amount").GetInt32());
        Assert.EndsWith($"/api/payments/{json.GetProperty("id").GetGuid()}", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Post_declined_payment_returns_201_with_declined_status()
    {
        var response = await _client.PostAsJsonAsync("/api/payments", Body(cardNumber: DeclinedCard));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await ReadJson(response);
        Assert.Equal("Declined", json.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Response_contains_only_safe_fields()
    {
        var response = await _client.PostAsJsonAsync("/api/payments", Body());

        var json = await ReadJson(response);
        Assert.Equal(
            ["id", "status", "card_number_last_four", "expiry_month", "expiry_year", "currency", "amount"],
            json.EnumerateObject().Select(p => p.Name));
    }

    [Fact]
    public async Task Post_invalid_payment_returns_400_rejected_and_skips_bank()
    {
        var response = await _client.PostAsJsonAsync("/api/payments", Body(cvv: "12", expiryYear: 2020));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await ReadJson(response);
        Assert.Equal("Rejected", json.GetProperty("status").GetString());
        Assert.Equal(2, json.GetProperty("errors").GetArrayLength());
        Assert.Empty(_bank.Requests);
    }

    [Fact]
    public async Task Post_malformed_json_returns_400_rejected_and_skips_bank()
    {
        var content = new StringContent("""{"card_number":"2222405343248877","amount":"abc"}""", Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/payments", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await ReadJson(response);
        Assert.Equal("Rejected", json.GetProperty("status").GetString());
        Assert.NotEqual(0, json.GetProperty("errors").GetArrayLength());
        Assert.Empty(_bank.Requests);
    }

    [Fact]
    public async Task Post_when_bank_unavailable_returns_502()
    {
        var response = await _client.PostAsJsonAsync("/api/payments", Body(cardNumber: BankUnavailableCard));

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

    [Fact]
    public async Task Get_returns_previously_created_payment()
    {
        var created = await ReadJson(await _client.PostAsJsonAsync("/api/payments", Body()));
        var id = created.GetProperty("id").GetGuid();

        var response = await _client.GetAsync($"/api/payments/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var fetched = await ReadJson(response);
        Assert.Equal(created.ToString(), fetched.ToString());
    }

    [Theory]
    [InlineData("3fa85f64-5717-4562-b3fc-2c963f66afa6")] // well-formed but unknown
    [InlineData("not-a-guid")]
    public async Task Get_unknown_payment_returns_404(string id)
    {
        var response = await _client.GetAsync($"/api/payments/{id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

internal sealed class FakeBankClient : IBankClient
{
    public List<BankPaymentRequest> Requests { get; } = [];

    public Task<AuthorizationResult> AuthorizeAsync(BankPaymentRequest request, CancellationToken ct = default)
    {
        Requests.Add(request);
        var lastDigit = request.CardNumber[^1] - '0';
        AuthorizationResult result = lastDigit switch
        {
            0 => new AuthorizationResult.Unavailable(),
            _ when lastDigit % 2 == 1 => new AuthorizationResult.Authorized(),
            _ => new AuthorizationResult.Declined()
        };
        return Task.FromResult(result);
    }
}

internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
