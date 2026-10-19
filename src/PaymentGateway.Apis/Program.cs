using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using PaymentGateway.Models.Domain;
using PaymentGateway.Models.Records;
using PaymentGateway.Repository;
using PaymentGateway.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRepository();
builder.Services.AddServices(builder.Configuration);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    })
    .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = RejectMalformedRequest);

var app = builder.Build();

app.MapControllers();

app.Run();

static IActionResult RejectMalformedRequest(ActionContext context)
{
    var errors = context.ModelState.Values
        .SelectMany(entry => entry.Errors)
        .Select(error => error.ErrorMessage)
        .ToList();

    return new BadRequestObjectResult(new RejectedPaymentResponse(PaymentStatus.Rejected, errors));
}
