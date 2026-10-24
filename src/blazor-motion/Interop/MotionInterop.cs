using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorMotion.Interop;

/// <summary>
/// Default <see cref="IMotionInterop"/>. Imports <c>blazor-motion.js</c> on first use and forwards payloads to it.
/// Only asynchronous JS interop is used, so it works under Server, WebAssembly and Auto render modes.
/// </summary>
internal sealed class MotionInterop : IMotionInterop, IAsyncDisposable
{
    private const string ModulePath = "./_content/BlazorMotion/blazor-motion.js";

    private readonly IJSRuntime _js;
    private readonly SemaphoreSlim _moduleLock = new(1, 1);
    private IJSObjectReference? _module;
    private bool _disposed;

    /// <summary>
    /// Creates the interop bridge. The module is not loaded until the first call.
    /// </summary>
    /// <param name="js">The JS runtime of the current circuit or WebAssembly host.</param>
    public MotionInterop(IJSRuntime js)
    {
        ArgumentNullException.ThrowIfNull(js);

        _js = js;
    }

    /// <inheritdoc />
    public async ValueTask<IMotionHandle> AnimateAsync(ElementReference target, MotionAnimation animation, CancellationToken cancellationToken = default)
    {
        var module = await GetModuleAsync(cancellationToken);
        var payload = MotionPayloadMapper.ToPayload(animation);
        var id = await module.InvokeAsync<int>("animate", cancellationToken, target, payload);

        return new MotionHandle(module, [id]);
    }

    /// <inheritdoc />
    public async ValueTask<IMotionHandle> PlayAsync(MotionTimeline timeline, CancellationToken cancellationToken = default)
    {
        var module = await GetModuleAsync(cancellationToken);
        var entries = MotionPayloadMapper.ToPayload(timeline);
        var ids = await module.InvokeAsync<int[]>("playTimeline", cancellationToken, entries);

        return new MotionHandle(module, ids);
    }

    /// <inheritdoc />
    public async ValueTask CancelAllAsync(ElementReference target, CancellationToken cancellationToken = default)
    {
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
    /// Imports the module once. The semaphore makes concurrent first calls wait for a single import.
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

            _module ??= await _js.InvokeAsync<IJSObjectReference>("import", cancellationToken, ModulePath);
            return _module;
        }
        finally
        {
            _moduleLock.Release();
        }
    }
}
