using System.Diagnostics;

namespace PaymentGateway.Models.Results;

public abstract record FindResult<T>
{
    private FindResult() { }

    public sealed record Found(T Value) : FindResult<T>;
    public sealed record NotFound : FindResult<T>;

    public TOut Match<TOut>(Func<T, TOut> found, Func<TOut> notFound) => this switch
    {
        Found f => found(f.Value),
        NotFound => notFound(),
        _ => throw new UnreachableException()
    };
}
