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
    public MotionAnimation WithTransition(MotionTransition transition) => throw new NotImplementedException();

    /// <summary>
    /// Returns the reverse of this animation (keyframes in opposite order, direction preserved).
    /// Useful for exit animations derived from enter animations.
    /// </summary>
    /// <returns>A new animation.</returns>
    public MotionAnimation Reversed() => throw new NotImplementedException();
}
