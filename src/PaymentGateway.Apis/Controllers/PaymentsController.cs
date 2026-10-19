using Microsoft.AspNetCore.Mvc;
using PaymentGateway.Models.Domain;
using PaymentGateway.Models.Interfaces.Services;
using PaymentGateway.Models.Records;

namespace PaymentGateway.Apis.Controllers;

[ApiController]
[Route("api/payments")]
public sealed class PaymentsController(IPaymentService payments) : ControllerBase
{
    private const string BankUnavailableMessage = "The bank is unavailable. The payment was not processed.";

    [HttpPost]
    [ProducesResponseType<PaymentResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<RejectedPaymentResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Post(PostPaymentRequest request, CancellationToken ct)
    {
        var result = await payments.ProcessAsync(request, ct);

        return result.Match<IActionResult>(
            processed: payment => CreatedAtAction(nameof(Get), new { id = payment.Id }, payment),
            rejected: errors => BadRequest(new RejectedPaymentResponse(PaymentStatus.Rejected, errors)),
            bankUnavailable: () => StatusCode(StatusCodes.Status502BadGateway, new { message = BankUnavailableMessage }));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<PaymentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var result = await payments.GetByIdAsync(id, ct);

        return result.Match<IActionResult>(
            found: payment => Ok(payment),
            notFound: () => NotFound(new { message = $"Payment {id} was not found." }));
    }
}
