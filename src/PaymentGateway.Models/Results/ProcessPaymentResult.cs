using System.Diagnostics;
using PaymentGateway.Models.Records;

namespace PaymentGateway.Models.Results;

public abstract record ProcessPaymentResult
{
    private ProcessPaymentResult() { }

    public sealed record Processed(PaymentResponse Payment) : ProcessPaymentResult;
    public sealed record Rejected(IReadOnlyList<string> Errors) : ProcessPaymentResult;
    public sealed record BankUnavailable : ProcessPaymentResult;

    public TOut Match<TOut>(
        Func<PaymentResponse, TOut> processed,
        Func<IReadOnlyList<string>, TOut> rejected,
        Func<TOut> bankUnavailable) => this switch
    {
        Processed p => processed(p.Payment),
        Rejected r => rejected(r.Errors),
        BankUnavailable => bankUnavailable(),
        _ => throw new UnreachableException()
    };
}
