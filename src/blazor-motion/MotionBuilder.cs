namespace BlazorMotion;

/// <summary>
/// Fluent entry point for describing an animation. Mutable while building; <see cref="Build"/> yields
/// an immutable <see cref="MotionAnimation"/>. Modelled after Framer Motion's <c>animate</c> props.
/// </summary>
/// <example>
/// <code>
/// var anim = new MotionBuilder()
///     .From(opacity: 0, y: 20)
///     .To(opacity: 1, y: 0)
///     .WithTransition(t => t.Duration(TimeSpan.FromMilliseconds(400)).Ease(MotionEasing.EaseOut))
///     .Build();
/// </code>
/// </example>
public sealed class MotionBuilder
{
    /// <summary>
    /// Defines the starting keyframe. Optional; when omitted the element's current computed style is used.
    /// </summary>
    /// <param name="x">translateX in px.</param>
    /// <param name="y">translateY in px.</param>
    /// <param name="scale">Uniform scale.</param>
    /// <param name="rotate">Rotation in degrees.</param>
    /// <param name="opacity">Opacity 0..1.</param>
    /// <returns>This builder.</returns>
    public MotionBuilder From(
        double? x = null,
        double? y = null,
        double? scale = null,
        double? rotate = null,
        double? opacity = null) => throw new NotImplementedException();

    /// <summary>
    /// Appends a target keyframe. Call several times to create multi-step animations.
    /// </summary>
    /// <param name="x">translateX in px.</param>
    /// <param name="y">translateY in px.</param>
    /// <param name="scale">Uniform scale.</param>
    /// <param name="rotate">Rotation in degrees.</param>
    /// <param name="opacity">Opacity 0..1.</param>
    /// <returns>This builder.</returns>
    public MotionBuilder To(
        double? x = null,
        double? y = null,
        double? scale = null,
        double? rotate = null,
        double? opacity = null) => throw new NotImplementedException();

    /// <summary>
    /// Appends a target keyframe with arbitrary CSS properties, for anything <see cref="To(double?, double?, double?, double?, double?)"/> does not cover.
    /// </summary>
    /// <param name="cssProperties">camelCase CSS property -> value.</param>
    /// <param name="offset">Position 0..1, or <c>null</c> for even distribution.</param>
    /// <returns>This builder.</returns>
    public MotionBuilder To(IReadOnlyDictionary<string, string> cssProperties, double? offset = null) => throw new NotImplementedException();

    /// <summary>
    /// Configures timing via a nested builder.
    /// </summary>
    /// <param name="configure">Callback receiving a <see cref="MotionTransitionBuilder"/>.</param>
    /// <returns>This builder.</returns>
    public MotionBuilder WithTransition(Action<MotionTransitionBuilder> configure) => throw new NotImplementedException();

    /// <summary>
    /// Uses a pre-built transition.
    /// </summary>
    /// <param name="transition">The transition.</param>
    /// <returns>This builder.</returns>
    public MotionBuilder WithTransition(MotionTransition transition) => throw new NotImplementedException();

    /// <summary>
    /// Shorthand for a spring-eased transition.
    /// </summary>
    /// <param name="stiffness">Spring stiffness.</param>
    /// <param name="damping">Damping.</param>
    /// <returns>This builder.</returns>
    public MotionBuilder Spring(double stiffness = 100, double damping = 10) => throw new NotImplementedException();

    /// <summary>
    /// Controls whether the final state persists after completion. Default <c>true</c>.
    /// </summary>
    /// <param name="fill">Keep final keyframe applied.</param>
    /// <returns>This builder.</returns>
    public MotionBuilder FillForwards(bool fill = true) => throw new NotImplementedException();

    /// <summary>
    /// Produces the immutable animation.
    /// </summary>
    /// <returns>A <see cref="MotionAnimation"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no <c>To</c> keyframe was added.</exception>
    public MotionAnimation Build() => throw new NotImplementedException();
}
