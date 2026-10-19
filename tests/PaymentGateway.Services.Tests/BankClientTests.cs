using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using PaymentGateway.Models.Records;
using PaymentGateway.Models.Results;
using PaymentGateway.Services.Clients;

namespace PaymentGateway.Services.Tests;

public class BankClientTests
{
    private static readonly BankPaymentRequest Request = new("2222405343248877", "04/2027", "GBP", 100, "123");

    [Fact]
    public async Task Sends_snake_case_json_to_the_payments_endpoint()
    {
        var handler = StubHandler.Returns(HttpStatusCode.OK, """{"authorized":true,"authorization_code":"abc"}""");

        await AuthorizeWith(handler);

        Assert.Equal("http://localhost:8080/payments", handler.SentUri?.ToString());
        Assert.Equal(
            """{"card_number":"2222405343248877","expiry_date":"04/2027","currency":"GBP","amount":100,"cvv":"123"}""",
            handler.SentBody);
    }

    [Fact]
    public async Task Authorized_reply_returns_authorized()
    {
        var result = await AuthorizeWith(
            StubHandler.Returns(HttpStatusCode.OK, """{"authorized":true,"authorization_code":"abc"}"""));

        Assert.IsType<AuthorizationResult.Authorized>(result);
    }

    [Fact]
    public async Task Unauthorized_reply_returns_declined()
    {
        var result = await AuthorizeWith(
            StubHandler.Returns(HttpStatusCode.OK, """{"authorized":false,"authorization_code":""}"""));

        Assert.IsType<AuthorizationResult.Declined>(result);
    }

    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task Error_status_returns_unavailable(HttpStatusCode status)
    {
        var result = await AuthorizeWith(StubHandler.Returns(status));

        Assert.IsType<AuthorizationResult.Unavailable>(result);
    }

    [Fact]
    public async Task Connection_failure_returns_unavailable()
    {
        var result = await AuthorizeWith(StubHandler.Throws(new HttpRequestException("Connection refused")));

        Assert.IsType<AuthorizationResult.Unavailable>(result);
    }

    [Fact]
    public async Task Unreadable_reply_returns_unavailable()
    {
        var result = await AuthorizeWith(StubHandler.Returns(HttpStatusCode.OK, "not json"));

        Assert.IsType<AuthorizationResult.Unavailable>(result);
    }

    private static Task<AuthorizationResult> AuthorizeWith(StubHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8080/") };
        var client = new BankClient(httpClient, NullLogger<BankClient>.Instance);
        return client.AuthorizeAsync(Request);
    }

    private sealed class StubHandler(Func<HttpResponseMessage> respond) : HttpMessageHandler
    {
        public Uri? SentUri { get; private set; }
        public string? SentBody { get; private set; }

        public static StubHandler Returns(HttpStatusCode status, string body = "") =>
            new(() => new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });

        public static StubHandler Throws(Exception exception) =>
            new(() => throw exception);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            SentUri = request.RequestUri;
            SentBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
            return respond();
        }
    }
}
