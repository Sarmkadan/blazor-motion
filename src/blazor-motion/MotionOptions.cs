namespace BlazorMotion;

/// <summary>
/// Global configuration for BlazorMotion, bound through the options pattern (<c>IOptions&lt;MotionOptions&gt;</c>).
/// Validated by <see cref="MotionOptionsValidator"/>.
/// </summary>
public sealed class MotionOptions
{
    /// <summary>
    /// Transition used when an animation does not specify its own timing. Must have a non-negative
    /// <see cref="MotionTransition.Duration"/> and <see cref="MotionTransition.Delay"/>.
    /// </summary>
    public MotionTransition DefaultTransition { get; set; } = MotionTransition.Default;

    /// <summary>
    /// Module specifier passed to the dynamic <c>import()</c> of the JS adapter. Must not be null or whitespace.
    /// Relative to the page's base URI; the default points at the static web asset served for this package.
    /// </summary>
    public string ModulePath { get; set; } = "./_content/BlazorMotion/blazor-motion.js";

    /// <summary>
    /// When <c>true</c>, the JS adapter is told to collapse animation durations to <c>0</c> while the
    /// <c>prefers-reduced-motion: reduce</c> media query matches. The flag is forwarded to the JS side;
    /// the collapse itself is implemented there.
    /// </summary>
    public bool RespectReducedMotion { get; set; } = true;
}
