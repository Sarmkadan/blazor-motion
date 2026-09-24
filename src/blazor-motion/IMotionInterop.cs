using Microsoft.AspNetCore.Components;

namespace BlazorMotion;

/// <summary>
/// Handle to a running animation in the browser. Disposing cancels it.
/// </summary>
public interface IMotionHandle : IAsyncDisposable
{
    /// <summary>Completes when the animation finishes naturally; faults with <see cref="OperationCanceledException"/> when cancelled.</summary>
    Task Finished { get; }

    /// <summary>Pauses playback.</summary>
    /// <param name="cancellationToken">Cancels the interop call, not the animation.</param>
    /// <returns>A task that completes when the browser acknowledged the pause.</returns>
    ValueTask PauseAsync(CancellationToken cancellationToken = default);

    /// <summary>Resumes playback after <see cref="PauseAsync"/>.</summary>
    /// <param name="cancellationToken">Cancels the interop call, not the animation.</param>
    /// <returns>A task that completes when the browser acknowledged the resume.</returns>
    ValueTask PlayAsync(CancellationToken cancellationToken = default);

    /// <summary>Cancels the animation and removes its effects.</summary>
    /// <param name="cancellationToken">Cancels the interop call.</param>
    /// <returns>A task that completes when the animation was cancelled.</returns>
    ValueTask CancelAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// The bridge between the C# animation model and the browser. This is the only abstraction that touches JS interop.
/// Register via <c>services.AddBlazorMotion()</c>; inject into components.
/// </summary>
public interface IMotionInterop
{
    /// <summary>
    /// Starts an animation on a single element.
    /// </summary>
    /// <param name="target">Element to animate. Must be rendered.</param>
    /// <param name="animation">The animation definition.</param>
    /// <param name="cancellationToken">Cancels the initial interop call.</param>
    /// <returns>A handle to control and await the animation.</returns>
    ValueTask<IMotionHandle> AnimateAsync(ElementReference target, MotionAnimation animation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts every entry of a timeline with the correct relative offsets.
    /// </summary>
    /// <param name="timeline">The timeline.</param>
    /// <param name="cancellationToken">Cancels the initial interop call.</param>
    /// <returns>A handle covering the whole timeline; <see cref="IMotionHandle.Finished"/> completes when the last entry ends.</returns>
    ValueTask<IMotionHandle> PlayAsync(MotionTimeline timeline, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels all animations currently running on an element.
    /// </summary>
    /// <param name="target">The element.</param>
    /// <param name="cancellationToken">Cancels the interop call.</param>
    /// <returns>A task that completes when the browser acknowledged.</returns>
    ValueTask CancelAllAsync(ElementReference target, CancellationToken cancellationToken = default);
}
