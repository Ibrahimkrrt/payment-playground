using System.Diagnostics;

namespace PaymentGateway.Models.Results;

public abstract record AuthorizationResult
{
    private AuthorizationResult() { }

    public sealed record Authorized : AuthorizationResult;
    public sealed record Declined : AuthorizationResult;
    public sealed record Unavailable : AuthorizationResult;

    public TOut Match<TOut>(Func<TOut> authorized, Func<TOut> declined, Func<TOut> unavailable) => this switch
    {
        Authorized => authorized(),
        Declined => declined(),
        Unavailable => unavailable(),
        _ => throw new UnreachableException()
    };
}
