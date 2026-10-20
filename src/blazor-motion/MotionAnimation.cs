namespace BlazorMotion;

/// <summary>
/// A single keyframe: a set of animatable CSS property values at one point in the animation.
/// Keys are CSS property names in camelCase (<c>transform</c>, <c>opacity</c>, <c>backgroundColor</c>), values are CSS strings.
/// </summary>
/// <param name="Properties">Property name -> CSS value.</param>
/// <param name="Offset">Position in the animation, 0..1, or <c>null</c> to distribute evenly.</param>
public sealed record MotionKeyframe(
    IReadOnlyDictionary<string, string> Properties,
    double? Offset = null);

/// <summary>
/// Complete, immutable description of one animation applied to one element: what to animate and how.
/// Produced by <see cref="MotionBuilder"/>, executed by <see cref="IMotionInterop"/>.
/// Serialises to the payload passed to the Web Animations API.
/// </summary>
/// <param name="Keyframes">Ordered keyframes. At least one.</param>
/// <param name="Transition">Timing parameters.</param>
/// <param name="FillForwards">Whether the final keyframe stays applied after the animation finishes.</param>
public sealed record MotionAnimation(
    IReadOnlyList<MotionKeyframe> Keyframes,
    MotionTransition Transition,
    bool FillForwards = true)
{
    /// <summary>
    /// Returns a copy with a different transition, keeping the keyframes.
    /// </summary>
    /// <param name="transition">The transition to use.</param>
    /// <returns>A new animation.</returns>
    public MotionAnimation WithTransition(MotionTransition transition)
    {
        ArgumentNullException.ThrowIfNull(transition);
        return this with { Transition = transition };
    }

    /// <summary>
    /// Returns a copy with the transition's delay replaced.
    /// </summary>
    /// <param name="delay">New delay. Must be non-negative.</param>
    /// <returns>A new animation with the adjusted delay.</returns>
    public MotionAnimation WithDelay(TimeSpan delay)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(delay.Ticks, nameof(delay));
        return this with { Transition = Transition with { Delay = delay } };
    }

    /// <summary>
    /// Returns a copy with the transition's duration replaced.
    /// </summary>
    /// <param name="duration">New duration. Must be non-negative.</param>
    /// <returns>A new animation with the adjusted duration.</returns>
    public MotionAnimation WithDuration(TimeSpan duration)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(duration.Ticks, nameof(duration));
        return this with { Transition = Transition with { Duration = duration } };
    }

    /// <summary>
    /// Returns the reverse of this animation (keyframes in opposite order, direction preserved).
    /// Useful for exit animations derived from enter animations.
    /// Keyframes with an explicit <see cref="MotionKeyframe.Offset"/> have it mapped to <c>1 - Offset</c>.
    /// </summary>
    /// <returns>A new animation.</returns>
    public MotionAnimation Reversed()
    {
        var reversed = Keyframes
            .Reverse()
            .Select(k => k with { Offset = k.Offset.HasValue ? 1.0 - k.Offset.Value : null })
            .ToList()
            .AsReadOnly();

        return this with { Keyframes = reversed };
    }

    /// <summary>
    /// Creates a <see cref="MotionAnimation"/> from an enumerable of keyframes.
    /// </summary>
    /// <param name="keyframes">One or more keyframes. Must not be empty.</param>
    /// <param name="transition">
    /// Timing parameters. When <c>null</c>, <see cref="MotionTransition.Default"/> is used.
    /// </param>
    /// <returns>A new animation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="keyframes"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="keyframes"/> is empty.</exception>
    public static MotionAnimation FromKeyframes(
        IEnumerable<MotionKeyframe> keyframes,
        MotionTransition? transition = null)
    {
        ArgumentNullException.ThrowIfNull(keyframes);

        var list = keyframes.ToList().AsReadOnly();
        if (list.Count == 0)
        {
            throw new ArgumentException("At least one keyframe is required.", nameof(keyframes));
        }

        return new MotionAnimation(list, transition ?? MotionTransition.Default);
    }
}
