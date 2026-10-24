using Microsoft.JSInterop;

namespace BlazorMotion.Interop;

/// <summary>
/// Controls one or more animations started through <c>blazor-motion.js</c>, identified by the ids it returned.
/// Does not own the module reference: it is shared by every handle of the same <see cref="MotionInterop"/>.
/// </summary>
internal sealed class MotionHandle : IMotionHandle
{
    private readonly IJSObjectReference _module;
    private readonly IReadOnlyList<int> _ids;
    private Task? _finished;

    /// <summary>
    /// Creates a handle over the given animation ids.
    /// </summary>
    /// <param name="module">The loaded <c>blazor-motion.js</c> module.</param>
    /// <param name="ids">Ids returned by <c>animate</c> or <c>playTimeline</c>.</param>
    public MotionHandle(IJSObjectReference module, IReadOnlyList<int> ids)
    {
        ArgumentNullException.ThrowIfNull(module);
        ArgumentNullException.ThrowIfNull(ids);

        _module = module;
        _ids = ids;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Created on first access, so no interop call is made until someone awaits it.
    /// Faults with <see cref="OperationCanceledException"/> if any animation was cancelled.
    /// </remarks>
    public Task Finished => _finished ??= WaitForFinishAsync();

    /// <inheritdoc />
    public async ValueTask PauseAsync(CancellationToken cancellationToken = default)
    {
        await InvokeForEachAsync("pause", cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask PlayAsync(CancellationToken cancellationToken = default)
    {
        await InvokeForEachAsync("play", cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask CancelAsync(CancellationToken cancellationToken = default)
    {
        await InvokeForEachAsync("cancel", cancellationToken);
    }

    /// <summary>
    /// Cancels the animations. Swallows <see cref="JSDisconnectedException"/>: if the circuit is gone, so are the animations.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            await CancelAsync();
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, so the browser-side animations are gone with it.
        }
    }

    private async Task WaitForFinishAsync()
    {
        var results = await Task.WhenAll(_ids.Select(id => _module.InvokeAsync<bool>("finished", id).AsTask()));

        if (results.Any(finished => !finished))
        {
            throw new OperationCanceledException("A motion animation was cancelled before it finished.");
        }
    }

    private async ValueTask InvokeForEachAsync(string method, CancellationToken cancellationToken)
    {
        foreach (var id in _ids)
        {
            await _module.InvokeVoidAsync(method, cancellationToken, id);
        }
    }
}
