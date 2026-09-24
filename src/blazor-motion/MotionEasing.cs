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

    /// <summary>
    /// Creates a custom cubic-bezier curve.
    /// </summary>
    /// <param name="x1">X of the first control point, 0..1.</param>
    /// <param name="y1">Y of the first control point.</param>
    /// <param name="x2">X of the second control point, 0..1.</param>
    /// <param name="y2">Y of the second control point.</param>
    /// <returns>An easing wrapping <c>cubic-bezier(x1, y1, x2, y2)</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="x1"/> or <paramref name="x2"/> is outside 0..1.</exception>
    public static MotionEasing CubicBezier(double x1, double y1, double x2, double y2)
    {
        throw new NotImplementedException();
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
