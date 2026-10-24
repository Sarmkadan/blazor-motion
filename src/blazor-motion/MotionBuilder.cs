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
    /// Creates an empty builder. Equivalent to <c>new MotionBuilder()</c>.
    /// </summary>
    /// <returns>A new, empty builder.</returns>
    public static MotionBuilder Create() => new();

    /// <summary>
    /// Preset: fades an element in from opacity 0 to 1.
    /// </summary>
    /// <returns>A builder with the preset keyframes; chain <c>WithTransition</c> to adjust timing.</returns>
    public static MotionBuilder FadeIn() => new MotionBuilder()
        .From(opacity: 0)
        .To(opacity: 1);

    /// <summary>
    /// Preset: fades an element out from opacity 1 to 0.
    /// </summary>
    /// <returns>A builder with the preset keyframes; chain <c>WithTransition</c> to adjust timing.</returns>
    public static MotionBuilder FadeOut() => new MotionBuilder()
        .From(opacity: 1)
        .To(opacity: 0);

    /// <summary>
    /// Preset: slides an element up into place while fading it in.
    /// </summary>
    /// <param name="distancePx">Starting distance below the final position, in px. Default 20.</param>
    /// <returns>A builder with the preset keyframes; chain <c>WithTransition</c> to adjust timing.</returns>
    public static MotionBuilder SlideUp(double distancePx = 20) => new MotionBuilder()
        .From(y: distancePx, opacity: 0)
        .To(y: 0, opacity: 1);

    /// <summary>
    /// Defines the starting keyframe. Optional; when omitted the element's current computed style is used.
    /// </summary>
    /// <param name="x">translateX in px.</param>
    /// <param name="y">translateY in px.</param>
    /// <param name="scale">Uniform scale.</param>
    /// <param name="rotate">Rotation in degrees.</param>
    /// <param name="opacity">Opacity 0..1.</param>
    /// <returns>This builder.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="opacity"/> is outside 0..1 (or NaN), or <paramref name="scale"/> is negative (or NaN).</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="x"/>, <paramref name="y"/> or <paramref name="rotate"/> is NaN or infinite.</exception>
    public MotionBuilder From(
        double? x = null,
        double? y = null,
        double? scale = null,
        double? rotate = null,
        double? opacity = null)
    {
        ValidateValues(x, y, scale, rotate, opacity);

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
    /// <exception cref="ArgumentException">Thrown when no property is given, or when <paramref name="x"/>, <paramref name="y"/> or <paramref name="rotate"/> is NaN or infinite.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="opacity"/> is outside 0..1 (or NaN), or <paramref name="scale"/> is negative (or NaN).</exception>
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

        ValidateValues(x, y, scale, rotate, opacity);

        _to.Add(new MotionKeyframe(BuildProperties(x, y, scale, rotate, opacity)));
        return this;
    }

    /// <summary>
    /// Appends a target keyframe with arbitrary CSS properties, for anything <see cref="To(double?, double?, double?, double?, double?)"/> does not cover.
    /// </summary>
    /// <param name="cssProperties">camelCase CSS property -> value.</param>
    /// <param name="offset">Position 0..1, or <c>null</c> for even distribution.</param>
    /// <returns>This builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="cssProperties"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="cssProperties"/> is empty, or any key is <c>null</c> or whitespace, or any value is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is outside 0..1 (or NaN).</exception>
    public MotionBuilder To(IReadOnlyDictionary<string, string> cssProperties, double? offset = null)
    {
        ArgumentNullException.ThrowIfNull(cssProperties);

        if (cssProperties.Count == 0)
        {
            throw new ArgumentException("At least one CSS property must be provided.", nameof(cssProperties));
        }

        foreach (var (key, value) in cssProperties)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("CSS property names must not be null or whitespace.", nameof(cssProperties));
            }

            if (value is null)
            {
                throw new ArgumentException($"CSS property '{key}' must not have a null value.", nameof(cssProperties));
            }
        }

        if (offset is { } o)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(o, 0, nameof(offset));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(o, 1, nameof(offset));
        }

        _to.Add(new MotionKeyframe(new Dictionary<string, string>(cssProperties), offset));
        return this;
    }

    /// <summary>
    /// Configures timing via a nested builder.
    /// </summary>
    /// <param name="configure">Callback receiving a <see cref="MotionTransitionBuilder"/>.</param>
    /// <returns>This builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <c>null</c>.</exception>
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
    /// <exception cref="ArgumentNullException"><paramref name="transition"/> is <c>null</c>.</exception>
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
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="stiffness"/> is not greater than 0 (or NaN), or <paramref name="damping"/> is negative (or NaN).</exception>
    public MotionBuilder Spring(double stiffness = 100, double damping = 10)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(stiffness, 0, nameof(stiffness));
        ArgumentOutOfRangeException.ThrowIfLessThan(damping, 0, nameof(damping));

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
    /// Produces the immutable animation. The builder remains usable afterwards; the result does not share state with it.
    /// </summary>
    /// <returns>A <see cref="MotionAnimation"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no <c>To</c> keyframe was added, or when explicit keyframe offsets are outside 0..1 or decrease.
    /// </exception>
    public MotionAnimation Build()
    {
        if (_to.Count == 0)
        {
            throw new InvalidOperationException("Add at least one To() keyframe before Build()");
        }

        var keyframes = new List<MotionKeyframe>(_to.Count + 1);
        if (_from is not null)
        {
            keyframes.Add(_from);
        }

        keyframes.AddRange(_to);

        double? previous = null;
        foreach (var keyframe in keyframes)
        {
            if (keyframe.Offset is not { } offset)
            {
                continue;
            }

            if (offset < 0 || offset > 1)
            {
                throw new InvalidOperationException($"Keyframe offset {Format(offset)} must be within 0..1.");
            }

            if (previous is { } last && offset < last)
            {
                throw new InvalidOperationException(
                    $"Keyframe offsets must be non-decreasing, but {Format(offset)} follows {Format(last)}.");
            }

            previous = offset;
        }

        return new MotionAnimation(keyframes.AsReadOnly(), _transition, _fillForwards);
    }

    /// <summary>
    /// Rejects values that cannot be rendered as valid CSS. Shared by <see cref="From"/> and <see cref="To(double?, double?, double?, double?, double?)"/>.
    /// NaN is caught by the range checks because <c>CompareTo</c> orders it below every number.
    /// </summary>
    private static void ValidateValues(double? x, double? y, double? scale, double? rotate, double? opacity)
    {
        if (x is { } tx && !double.IsFinite(tx))
        {
            throw new ArgumentException("Must be a finite number.", nameof(x));
        }

        if (y is { } ty && !double.IsFinite(ty))
        {
            throw new ArgumentException("Must be a finite number.", nameof(y));
        }

        if (rotate is { } r && !double.IsFinite(r))
        {
            throw new ArgumentException("Must be a finite number.", nameof(rotate));
        }

        if (scale is { } s)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(s, 0, nameof(scale));
        }

        if (opacity is { } o)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(o, 0, nameof(opacity));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(o, 1, nameof(opacity));
        }
    }

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
