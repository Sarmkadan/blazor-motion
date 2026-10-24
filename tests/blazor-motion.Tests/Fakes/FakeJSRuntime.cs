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

    private readonly Dictionary<int, FinishedGate> _finishedGates = [];

    /// <summary>
    /// Holds the <c>finished</c> call for <paramref name="animationId"/> until the test calls <see cref="FinishedGate.Resolve"/>.
    /// Takes precedence over <see cref="Results"/> for that id.
    /// </summary>
    /// <param name="animationId">The id the handle will pass to <c>finished</c>.</param>
    public FinishedGate GateFinished(int animationId)
    {
        var gate = new FinishedGate();
        _finishedGates[animationId] = gate;
        return gate;
    }

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

        if (identifier == "finished" && args is [int id] && _finishedGates.TryGetValue(id, out var gate))
        {
            return FakeResult.Cast<TValue>(await gate.AwaitOutcomeAsync());
        }

        return FakeResult.Cast<TValue>(Results.TryGetValue(identifier, out var result) ? result(args) : null);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Controls one pending <c>finished</c> promise. The fake holds the call until <see cref="Resolve"/> is called;
/// <see cref="Reached"/> completes once the call is waiting on it, so tests can assert "still pending" without timing.
/// </summary>
public sealed class FinishedGate
{
    private readonly TaskCompletionSource<bool> _outcome = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _reached = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Completes once the <c>finished</c> call is waiting on this gate.</summary>
    public Task Reached => _reached.Task;

    /// <summary>Resolves the pending <c>finished</c> call: <c>true</c> for a finished animation, <c>false</c> for a cancelled one.</summary>
    /// <param name="finished">The value the JS promise resolves to.</param>
    public void Resolve(bool finished) => _outcome.TrySetResult(finished);

    internal Task<bool> AwaitOutcomeAsync()
    {
        _reached.TrySetResult();
        return _outcome.Task;
    }
}

/// <summary>Converts canned results to the type the caller asked for.</summary>
internal static class FakeResult
{
    /// <summary>Unboxes a canned result. <c>null</c> yields the default of <typeparamref name="TValue"/>, which covers void calls.</summary>
    internal static TValue Cast<TValue>(object? value) => value is null ? default! : (TValue)value;
}
