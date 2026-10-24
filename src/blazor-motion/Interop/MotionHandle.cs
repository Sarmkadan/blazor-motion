using Microsoft.JSInterop;

namespace BlazorMotion.Interop;

/// <summary>
/// Controls one or more animations started through <c>blazor-motion.js</c>, identified by the ids it returned.
/// Does not own the module reference: it is shared by every handle of the same <see cref="MotionInterop"/>.
/// </summary>
internal sealed class MotionHandle : IMotionHandle
{
    private readonly IJSObjectReference _module;
    private readonly int[] _animationIds;
    private readonly TaskCompletionSource _finished = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool _cancelled;

    /// <summary>
    /// Creates a handle over the given animation ids and starts watching them for completion.
    /// </summary>
    /// <param name="module">The loaded <c>blazor-motion.js</c> module.</param>
    /// <param name="animationIds">Ids returned by <c>animate</c> or <c>playTimeline</c>. Must not be empty.</param>
    /// <exception cref="ArgumentNullException"><paramref name="module"/> or <paramref name="animationIds"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="animationIds"/> is empty.</exception>
    public MotionHandle(IJSObjectReference module, IReadOnlyList<int> animationIds)
    {
        ArgumentNullException.ThrowIfNull(module);
        ArgumentNullException.ThrowIfNull(animationIds);
        if (animationIds.Count == 0)
        {
            throw new ArgumentException("At least one animation id is required.", nameof(animationIds));
        }

        _module = module;
        _animationIds = [.. animationIds];

        _ = WatchFinishedAsync();
    }

    /// <summary>
    /// The browser-side ids this handle controls. Exposed for tests.
    /// </summary>
    internal IReadOnlyList<int> AnimationIds => _animationIds;

    /// <inheritdoc />
    /// <remarks>
    /// Completes when every animation has finished. Faults with <see cref="OperationCanceledException"/>
    /// if any animation was cancelled.
    /// </remarks>
    public Task Finished => _finished.Task;

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">The animation was already cancelled.</exception>
    public async ValueTask PauseAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfCancelled();

        await InvokeForEachAsync("pause", cancellationToken);
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">The animation was already cancelled.</exception>
    public async ValueTask PlayAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfCancelled();

        await InvokeForEachAsync("play", cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask CancelAsync(CancellationToken cancellationToken = default)
    {
        await InvokeForEachAsync("cancel", cancellationToken);

        _cancelled = true;
    }

    /// <summary>
    /// Cancels the animations unless already cancelled or finished. Swallows <see cref="JSDisconnectedException"/>
    /// and <see cref="ObjectDisposedException"/>: if the circuit or module is gone, so are the animations.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_cancelled || _finished.Task.IsCompleted)
        {
            return;
        }

        try
        {
            await CancelAsync();
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, so the browser-side animations are gone with it.
        }
        catch (ObjectDisposedException)
        {
            // The module was disposed with the interop, so the animations are gone with it.
        }
    }

    /// <summary>
    /// Awaits every <c>finished</c> promise and completes <see cref="Finished"/> from the outcome.
    /// Never throws: any failure is stored in <see cref="Finished"/> instead.
    /// </summary>
    private async Task WatchFinishedAsync()
    {
        try
        {
            var results = await Task.WhenAll(_animationIds.Select(id => _module.InvokeAsync<bool>("finished", id).AsTask()));

            if (results.Any(finished => !finished))
            {
                _finished.TrySetException(new OperationCanceledException("A motion animation was cancelled before it finished."));
            }
            else
            {
                _finished.TrySetResult();
            }
        }
        catch (Exception exception)
        {
            _finished.TrySetException(exception);
        }
    }

    /// <exception cref="InvalidOperationException">The animation was already cancelled.</exception>
    private void ThrowIfCancelled()
    {
        if (_cancelled)
        {
            throw new InvalidOperationException("Animation was cancelled");
        }
    }

    private async ValueTask InvokeForEachAsync(string method, CancellationToken cancellationToken)
    {
        foreach (var id in _animationIds)
        {
            await _module.InvokeVoidAsync(method, cancellationToken, id);
        }
    }
}
