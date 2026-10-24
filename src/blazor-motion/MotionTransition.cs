namespace BlazorMotion;

/// <summary>
/// Playback direction of a repeated animation.
/// </summary>
public enum MotionDirection
{
    /// <summary>Always play forwards.</summary>
    Normal,

    /// <summary>Always play backwards.</summary>
    Reverse,

    /// <summary>Alternate forwards and backwards on each iteration.</summary>
    Alternate,
}

/// <summary>
/// Timing parameters of an animation: how long, how delayed, which curve, how many times.
/// Immutable; use <c>with</c> or <see cref="MotionTransitionBuilder"/> to derive variants.
/// </summary>
/// <param name="Duration">Length of one iteration.</param>
/// <param name="Delay">Time to wait before the first iteration starts.</param>
/// <param name="Easing">Timing function.</param>
/// <param name="Repeat">Number of additional iterations. <c>0</c> plays once; <see cref="InfiniteRepeat"/> loops forever.</param>
/// <param name="Direction">Playback direction across iterations.</param>
public sealed record MotionTransition(
    TimeSpan Duration,
    TimeSpan Delay,
    MotionEasing Easing,
    int Repeat,
    MotionDirection Direction)
{
    /// <summary>
    /// Value of <see cref="Repeat"/> that loops the animation indefinitely.
    /// </summary>
    public const int InfiniteRepeat = int.MaxValue;

    /// <summary>300 ms, no delay, ease-in-out, single run. Cached; records are value-equal, so sharing is safe.</summary>
    public static MotionTransition Default { get; } = new(
        Duration: TimeSpan.FromMilliseconds(300),
        Delay: TimeSpan.Zero,
        Easing: MotionEasing.EaseInOut,
        Repeat: 0,
        Direction: MotionDirection.Normal);
}

/// <summary>
/// Fluent builder for <see cref="MotionTransition"/>. Used inside <see cref="MotionBuilder.WithTransition(Action{MotionTransitionBuilder})"/>.
/// </summary>
public sealed class MotionTransitionBuilder
{
    /// <summary>Sets the duration of one iteration.</summary>
    /// <param name="duration">Must be non-negative.</param>
    /// <returns>This builder.</returns>
    public MotionTransitionBuilder Duration(TimeSpan duration) => throw new NotImplementedException();

    /// <summary>Sets the delay before the first iteration.</summary>
    /// <param name="delay">Must be non-negative.</param>
    /// <returns>This builder.</returns>
    public MotionTransitionBuilder Delay(TimeSpan delay) => throw new NotImplementedException();

    /// <summary>Sets the timing function.</summary>
    /// <param name="easing">Any <see cref="MotionEasing"/>.</param>
    /// <returns>This builder.</returns>
    public MotionTransitionBuilder Ease(MotionEasing easing) => throw new NotImplementedException();

    /// <summary>Repeats the animation a number of extra times.</summary>
    /// <param name="count">Additional iterations. Use <see cref="Forever"/> to loop.</param>
    /// <param name="direction">Direction across iterations.</param>
    /// <returns>This builder.</returns>
    public MotionTransitionBuilder Repeat(int count, MotionDirection direction = MotionDirection.Normal) => throw new NotImplementedException();

    /// <summary>Loops the animation indefinitely.</summary>
    /// <param name="direction">Direction across iterations.</param>
    /// <returns>This builder.</returns>
    public MotionTransitionBuilder Forever(MotionDirection direction = MotionDirection.Alternate) => throw new NotImplementedException();

    /// <summary>Materialises the immutable transition.</summary>
    /// <returns>The configured <see cref="MotionTransition"/>.</returns>
    public MotionTransition Build() => throw new NotImplementedException();
}
