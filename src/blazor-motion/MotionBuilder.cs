using System.Globalization;

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
    private MotionKeyframe? _from;
    private readonly List<MotionKeyframe> _to = new();
    private MotionTransition _transition = MotionTransition.Default;
    private bool _fillForwards = true;

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
        double? opacity = null)
    {
        _from = new MotionKeyframe(BuildProperties(x, y, scale, rotate, opacity), Offset: 0);
        return this;
    }

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
        double? opacity = null)
    {
        if (x is null && y is null && scale is null && rotate is null && opacity is null)
        {
            throw new ArgumentException("At least one property must be set");
        }

        _to.Add(new MotionKeyframe(BuildProperties(x, y, scale, rotate, opacity)));
        return this;
    }

    /// <summary>
    /// Appends a target keyframe with arbitrary CSS properties, for anything <see cref="To(double?, double?, double?, double?, double?)"/> does not cover.
    /// </summary>
    /// <param name="cssProperties">camelCase CSS property -> value.</param>
    /// <param name="offset">Position 0..1, or <c>null</c> for even distribution.</param>
    /// <returns>This builder.</returns>
    public MotionBuilder To(IReadOnlyDictionary<string, string> cssProperties, double? offset = null)
    {
        ArgumentNullException.ThrowIfNull(cssProperties);

        _to.Add(new MotionKeyframe(new Dictionary<string, string>(cssProperties), offset));
        return this;
    }

    /// <summary>
    /// Configures timing via a nested builder.
    /// </summary>
    /// <param name="configure">Callback receiving a <see cref="MotionTransitionBuilder"/>.</param>
    /// <returns>This builder.</returns>
    public MotionBuilder WithTransition(Action<MotionTransitionBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new MotionTransitionBuilder(_transition);
        configure(builder);
        _transition = builder.Build();
        return this;
    }

    /// <summary>
    /// Uses a pre-built transition.
    /// </summary>
    /// <param name="transition">The transition.</param>
    /// <returns>This builder.</returns>
    public MotionBuilder WithTransition(MotionTransition transition)
    {
        ArgumentNullException.ThrowIfNull(transition);

        _transition = transition;
        return this;
    }

    /// <summary>
    /// Shorthand for a spring-eased transition.
    /// </summary>
    /// <param name="stiffness">Spring stiffness.</param>
    /// <param name="damping">Damping.</param>
    /// <returns>This builder.</returns>
    public MotionBuilder Spring(double stiffness = 100, double damping = 10)
    {
        var easing = MotionEasing.Spring(stiffness, damping);
        _transition = _transition with
        {
            Easing = easing,
            Duration = easing.SuggestedDuration ?? _transition.Duration,
        };
        return this;
    }

    /// <summary>
    /// Controls whether the final state persists after completion. Default <c>true</c>.
    /// </summary>
    /// <param name="fill">Keep final keyframe applied.</param>
    /// <returns>This builder.</returns>
    public MotionBuilder FillForwards(bool fill = true)
    {
        _fillForwards = fill;
        return this;
    }

    /// <summary>
    /// Produces the immutable animation.
    /// </summary>
    /// <returns>A <see cref="MotionAnimation"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no <c>To</c> keyframe was added.</exception>
    public MotionAnimation Build() => throw new NotImplementedException();

    /// <summary>
    /// Maps the transform-related and opacity arguments to CSS properties. Only non-null parts are emitted.
    /// </summary>
    private static Dictionary<string, string> BuildProperties(
        double? x,
        double? y,
        double? scale,
        double? rotate,
        double? opacity)
    {
        var properties = new Dictionary<string, string>();

        var transform = new List<string>();
        if (x is { } tx)
        {
            transform.Add($"translateX({Format(tx)}px)");
        }

        if (y is { } ty)
        {
            transform.Add($"translateY({Format(ty)}px)");
        }

        if (scale is { } s)
        {
            transform.Add($"scale({Format(s)})");
        }

        if (rotate is { } r)
        {
            transform.Add($"rotate({Format(r)}deg)");
        }

        if (transform.Count > 0)
        {
            properties["transform"] = string.Join(' ', transform);
        }

        if (opacity is { } o)
        {
            properties["opacity"] = Format(o);
        }

        return properties;
    }

    private static string Format(double value) => value.ToString(CultureInfo.InvariantCulture);
}
