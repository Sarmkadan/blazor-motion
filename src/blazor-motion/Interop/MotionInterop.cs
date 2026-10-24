using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace BlazorMotion.Interop;

/// <summary>
/// Default <see cref="IMotionInterop"/>. Imports <c>blazor-motion.js</c> on first use and forwards payloads to it.
/// Only asynchronous JS interop is used, so it works under Server, WebAssembly and Auto render modes.
/// </summary>
internal sealed class MotionInterop : IMotionInterop, IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly IOptions<MotionOptions> _options;
    private readonly SemaphoreSlim _moduleLock = new(1, 1);
    private IJSObjectReference? _module;
    private bool _disposed;

    /// <summary>
    /// Creates the interop bridge. The module is not loaded until the first call.
    /// </summary>
    /// <param name="js">The JS runtime of the current circuit or WebAssembly host.</param>
    /// <param name="options">Supplies the module path and the global settings forwarded to JS.</param>
    /// <exception cref="ArgumentNullException"><paramref name="js"/> or <paramref name="options"/> is <see langword="null"/>.</exception>
    public MotionInterop(IJSRuntime js, IOptions<MotionOptions> options)
    {
        ArgumentNullException.ThrowIfNull(js);
        ArgumentNullException.ThrowIfNull(options);

        _js = js;
        _options = options;
    }

    /// <inheritdoc />
    /// <exception cref="ObjectDisposedException">This interop has been disposed.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="animation"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="animation"/> has no keyframes, or <paramref name="target"/> is not rendered yet
    /// (its <see cref="ElementReference.Id"/> is null or empty).
    /// </exception>
    public async ValueTask<IMotionHandle> AnimateAsync(ElementReference target, MotionAnimation animation, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(animation);
        if (animation.Keyframes.Count == 0)
        {
            throw new ArgumentException("Animation has no keyframes.", nameof(animation));
        }

        ValidateRendered(target);

        var module = await GetModuleAsync(cancellationToken);
        var payload = MotionPayloadMapper.ToPayload(animation);
        var id = await module.InvokeAsync<int>("animate", cancellationToken, target, payload);

        return new MotionHandle(module, [id]);
    }

    /// <inheritdoc />
    /// <exception cref="ObjectDisposedException">This interop has been disposed.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="timeline"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="timeline"/> has no entries.</exception>
    public async ValueTask<IMotionHandle> PlayAsync(MotionTimeline timeline, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(timeline);
        if (timeline.Entries.Count == 0)
        {
            throw new ArgumentException("Timeline has no entries");
        }

        var module = await GetModuleAsync(cancellationToken);
        var entries = MotionPayloadMapper.ToPayload(timeline);
        var ids = await module.InvokeAsync<int[]>("playTimeline", cancellationToken, entries);

        return new MotionHandle(module, ids);
    }

    /// <inheritdoc />
    /// <exception cref="ObjectDisposedException">This interop has been disposed.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="target"/> is not rendered yet (its <see cref="ElementReference.Id"/> is null or empty).
    /// </exception>
    public async ValueTask CancelAllAsync(ElementReference target, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ValidateRendered(target);

        var module = await GetModuleAsync(cancellationToken);

        await module.InvokeVoidAsync("cancelAll", cancellationToken, target);
    }

    /// <summary>
    /// Disposes the imported module. Swallows <see cref="JSDisconnectedException"/> because a Blazor Server
    /// circuit that has already gone takes the module with it.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Wait for an in-flight import so its module is not leaked.
        await _moduleLock.WaitAsync();
        try
        {
            if (_module is { } module)
            {
                _module = null;
                await module.DisposeAsync();
            }
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, so the module is gone with it.
        }
        finally
        {
            _moduleLock.Release();
        }
    }

    /// <summary>
    /// Rejects an element that has no id yet, i.e. a default <see cref="ElementReference"/> captured before the element rendered.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="target"/> is not rendered yet.</exception>
    private static void ValidateRendered(ElementReference target)
    {
        if (string.IsNullOrEmpty(target.Id))
        {
            throw new ArgumentException("Element is not rendered yet; call after OnAfterRenderAsync");
        }
    }

    /// <summary>
    /// Imports the module once and passes it the global settings. The semaphore makes concurrent first calls wait for a single import.
    /// </summary>
    private async ValueTask<IJSObjectReference> GetModuleAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_module is { } loaded)
        {
            return loaded;
        }

        await _moduleLock.WaitAsync(cancellationToken);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_module is null)
            {
                var options = _options.Value;
                var module = await _js.InvokeAsync<IJSObjectReference>("import", cancellationToken, options.ModulePath);
                await module.InvokeVoidAsync("configure", cancellationToken, new { respectReducedMotion = options.RespectReducedMotion });
                _module = module;
            }

            return _module;
        }
        finally
        {
            _moduleLock.Release();
        }
    }
}
