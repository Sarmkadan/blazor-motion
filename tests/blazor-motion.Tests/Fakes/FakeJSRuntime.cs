using Microsoft.JSInterop;

namespace BlazorMotion.Tests.Fakes;

/// <summary>One recorded JS interop call: the identifier and the arguments, without the cancellation token.</summary>
/// <param name="Identifier">The JS function or module member name.</param>
/// <param name="Args">The arguments passed to it.</param>
public sealed record JSCall(string Identifier, object?[]? Args);

/// <summary>
/// Test double for <see cref="IJSRuntime"/>. Records every call in <see cref="Calls"/> and answers from <see cref="Results"/>,
/// keyed by identifier. <c>import</c> answers with <see cref="Module"/> unless a result is set for it.
/// A call without a result returns the default value, so void calls need no setup.
/// Each call yields once before completing, so concurrent callers really interleave, as they would over a real circuit.
/// </summary>
public sealed class FakeJSRuntime : IJSRuntime
{
    /// <summary>Every call in order, including <c>import</c>. Guarded by its own lock because module watchers may record concurrently.</summary>
    public List<JSCall> Calls { get; } = [];

    /// <summary>Canned results by identifier. Receives the call's arguments and returns the value to hand back.</summary>
    public Dictionary<string, Func<object?[]?, object?>> Results { get; } = [];

    /// <summary>The module handed out for every <c>import</c> call.</summary>
    public FakeJSObjectReference Module { get; } = new();

    /// <inheritdoc />
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
        InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    /// <inheritdoc />
    public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (Calls)
        {
            Calls.Add(new JSCall(identifier, args));
        }

        await Task.Yield();

        return FakeResult.Cast<TValue>(Resolve(identifier, args));
    }

    private object? Resolve(string identifier, object?[]? args)
    {
        if (Results.TryGetValue(identifier, out var result))
        {
            return result(args);
        }

        return identifier == "import" ? Module : null;
    }
}

/// <summary>
/// Test double for a module returned by <c>import</c>. Records calls in <see cref="Calls"/> and answers from <see cref="Results"/>.
/// Defaults: <c>animate</c> returns 7, <c>playTimeline</c> returns <c>[1, 2, 3]</c>, <c>finished</c> returns <c>true</c>.
/// </summary>
public sealed class FakeJSObjectReference : IJSObjectReference
{
    /// <summary>Every call in order. Guarded by its own lock because handles watch <c>finished</c> in the background.</summary>
    public List<JSCall> Calls { get; } = [];

    /// <summary>Canned results by identifier. Receives the call's arguments and returns the value to hand back.</summary>
    public Dictionary<string, Func<object?[]?, object?>> Results { get; } = new()
    {
        ["animate"] = _ => 7,
        ["playTimeline"] = _ => new[] { 1, 2, 3 },
        ["finished"] = _ => true,
    };

    /// <summary>True once <see cref="DisposeAsync"/> has run.</summary>
    public bool IsDisposed { get; private set; }

    /// <inheritdoc />
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
        InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    /// <inheritdoc />
    public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (Calls)
        {
            Calls.Add(new JSCall(identifier, args));
        }

        await Task.Yield();

        return FakeResult.Cast<TValue>(Results.TryGetValue(identifier, out var result) ? result(args) : null);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        return ValueTask.CompletedTask;
    }
}

/// <summary>Converts canned results to the type the caller asked for.</summary>
internal static class FakeResult
{
    /// <summary>Unboxes a canned result. <c>null</c> yields the default of <typeparamref name="TValue"/>, which covers void calls.</summary>
    internal static TValue Cast<TValue>(object? value) => value is null ? default! : (TValue)value;
}
