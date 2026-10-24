using System.Globalization;

namespace BlazorMotion;

/// <summary>
/// Describes the timing function of an animation. Either a named preset or a cubic-bezier curve.
/// Maps directly onto a CSS <c>easing</c> value at the interop boundary.
/// </summary>
public readonly record struct MotionEasing
{
    private MotionEasing(string cssValue)
    {
        CssValue = cssValue;
    }

    /// <summary>The CSS timing-function string sent to the browser, e.g. <c>ease-in-out</c> or <c>cubic-bezier(0.4, 0, 0.2, 1)</c>.</summary>
    public string CssValue { get; }

    /// <summary>Constant speed.</summary>
    public static MotionEasing Linear => new("linear");

    /// <summary>Slow start.</summary>
    public static MotionEasing EaseIn => new("ease-in");

    /// <summary>Slow end.</summary>
    public static MotionEasing EaseOut => new("ease-out");

    /// <summary>Slow start and end.</summary>
    public static MotionEasing EaseInOut => new("ease-in-out");

    /// <summary>Starts slow, accelerates (<c>cubic-bezier(0.11, 0, 0.5, 0)</c>).</summary>
    public static MotionEasing EaseInQuad => CubicBezier(0.11, 0, 0.5, 0);

    /// <summary>Starts fast, decelerates smoothly (<c>cubic-bezier(0.5, 1, 0.89, 1)</c>).</summary>
    public static MotionEasing EaseOutQuad => CubicBezier(0.5, 1, 0.89, 1);

    /// <summary>Smooth acceleration and deceleration (<c>cubic-bezier(0.65, 0, 0.35, 1)</c>).</summary>
    public static MotionEasing EaseInOutCubic => CubicBezier(0.65, 0, 0.35, 1);

    /// <summary>Pulls back briefly before moving forward (<c>cubic-bezier(0.36, 0, 0.66, -0.56)</c>).</summary>
    public static MotionEasing Anticipate => CubicBezier(0.36, 0, 0.66, -0.56);

    /// <summary>Overshoots the target and settles back (<c>cubic-bezier(0.34, 1.56, 0.64, 1)</c>).</summary>
    public static MotionEasing BackOut => CubicBezier(0.34, 1.56, 0.64, 1);

    /// <summary>
    /// Creates a custom cubic-bezier curve.
    /// </summary>
    /// <param name="x1">X of the first control point, 0..1.</param>
    /// <param name="y1">Y of the first control point.</param>
    /// <param name="x2">X of the second control point, 0..1.</param>
    /// <param name="y2">Y of the second control point.</param>
    /// <returns>An easing wrapping <c>cubic-bezier(x1, y1, x2, y2)</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="x1"/> or <paramref name="x2"/> is outside 0..1.</exception>
    /// <exception cref="ArgumentException">Thrown when any argument is NaN or infinite.</exception>
    public static MotionEasing CubicBezier(double x1, double y1, double x2, double y2)
    {
        RequireFinite(x1, nameof(x1));
        RequireFinite(y1, nameof(y1));
        RequireFinite(x2, nameof(x2));
        RequireFinite(y2, nameof(y2));

        if (x1 < 0 || x1 > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(x1), x1, "Must be within 0..1.");
        }

        if (x2 < 0 || x2 > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(x2), x2, "Must be within 0..1.");
        }

        var css = string.Format(
            CultureInfo.InvariantCulture,
            "cubic-bezier({0:R}, {1:R}, {2:R}, {3:R})",
            x1, y1, x2, y2);

        return new MotionEasing(css);
    }

    private static void RequireFinite(double value, string paramName)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new ArgumentException("Must be a finite number.", paramName);
        }
    }

    /// <summary>
    /// Approximates a spring with the given physical parameters as a sampled easing curve.
    /// </summary>
    /// <param name="stiffness">Spring stiffness. Default 100.</param>
    /// <param name="damping">Damping ratio. Default 10.</param>
    /// <param name="mass">Mass. Default 1.</param>
    /// <returns>An easing that emulates spring motion.</returns>
    public static MotionEasing Spring(double stiffness = 100, double damping = 10, double mass = 1)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public override string ToString() => CssValue;
}
