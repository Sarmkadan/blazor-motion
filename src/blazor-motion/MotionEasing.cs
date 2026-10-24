using System.Globalization;

namespace BlazorMotion;

/// <summary>
/// Describes the timing function of an animation. Either a named preset or a cubic-bezier curve.
/// Maps directly onto a CSS <c>easing</c> value at the interop boundary.
/// </summary>
public readonly record struct MotionEasing
{
    private const double SpringSettleTolerance = 0.001;
    private const int SpringMaxMilliseconds = 10_000;
    private const int SpringSampleCount = 60;

    private MotionEasing(string cssValue, TimeSpan? suggestedDuration = null)
    {
        CssValue = cssValue;
        SuggestedDuration = suggestedDuration;
    }

    /// <summary>The CSS timing-function string sent to the browser, e.g. <c>ease-in-out</c> or <c>cubic-bezier(0.4, 0, 0.2, 1)</c>.</summary>
    public string CssValue { get; }

    /// <summary>
    /// The time the simulated motion needs to settle, for easings derived from physics (see <see cref="Spring"/>).
    /// <c>null</c> for every other easing, where the duration is chosen by the transition.
    /// </summary>
    public TimeSpan? SuggestedDuration { get; }

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
    /// <returns>
    /// An easing wrapping <c>linear(...)</c> with 60 samples of a damped harmonic oscillator,
    /// whose <see cref="SuggestedDuration"/> is the time it takes to settle (capped at 10 seconds).
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="stiffness"/> or <paramref name="mass"/> is not a positive finite number, or <paramref name="damping"/> is negative or not finite.</exception>
    public static MotionEasing Spring(double stiffness = 100, double damping = 10, double mass = 1)
    {
        if (!double.IsFinite(stiffness) || stiffness <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stiffness), stiffness, "Must be a positive finite number.");
        }

        if (!double.IsFinite(damping) || damping < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(damping), damping, "Must be a non-negative finite number.");
        }

        if (!double.IsFinite(mass) || mass <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(mass), mass, "Must be a positive finite number.");
        }

        var omega0 = Math.Sqrt(stiffness / mass);
        var zeta = damping / (2 * Math.Sqrt(stiffness * mass));

        var duration = FindSpringSettleTime(omega0, zeta);
        var seconds = duration.TotalSeconds;

        var samples = new string[SpringSampleCount];
        for (var i = 0; i < SpringSampleCount - 1; i++)
        {
            var t = seconds * i / (SpringSampleCount - 1);
            var value = Math.Round(SpringPosition(t, omega0, zeta), 4);

            // Normalise -0 to 0 so the output never reads "-0.0000".
            if (value == 0)
            {
                value = 0;
            }

            samples[i] = value.ToString("0.0000", CultureInfo.InvariantCulture);
        }

        // The last sample is the target itself; x(duration) is within the settle tolerance by definition.
        samples[SpringSampleCount - 1] = "1";

        var css = "linear(" + string.Join(", ", samples) + ")";

        return new MotionEasing(css, duration);
    }

    /// <summary>
    /// Scans the step response at 1 ms resolution and returns the earliest time after which
    /// the position stays within <see cref="SpringSettleTolerance"/> of the target, capped at 10 seconds.
    /// </summary>
    private static TimeSpan FindSpringSettleTime(double omega0, double zeta)
    {
        var lastViolation = -1;

        for (var ms = 0; ms <= SpringMaxMilliseconds; ms++)
        {
            var position = SpringPosition(ms / 1000.0, omega0, zeta);

            if (Math.Abs(1 - position) >= SpringSettleTolerance)
            {
                lastViolation = ms;
            }
        }

        return lastViolation >= SpringMaxMilliseconds
            ? TimeSpan.FromMilliseconds(SpringMaxMilliseconds)
            : TimeSpan.FromMilliseconds(lastViolation + 1);
    }

    /// <summary>
    /// Analytic step response of a damped harmonic oscillator released from rest at 0 towards a target of 1.
    /// The overdamped branch is written with decaying exponentials only, so it stays finite for very large damping.
    /// </summary>
    private static double SpringPosition(double t, double omega0, double zeta)
    {
        if (zeta < 1)
        {
            // Underdamped.
            var omegaD = omega0 * Math.Sqrt(1 - zeta * zeta);
            var decay = Math.Exp(-zeta * omega0 * t);
            return 1 - decay * (Math.Cos(omegaD * t) + zeta * omega0 / omegaD * Math.Sin(omegaD * t));
        }

        if (zeta - 1 < 1e-9)
        {
            // Critically damped.
            var decay = Math.Exp(-omega0 * t);
            return 1 - decay * (1 + omega0 * t);
        }

        // Overdamped.
        var a = zeta * omega0;
        var s = omega0 * Math.Sqrt(zeta * zeta - 1);
        var slow = (a + s) / (2 * s) * Math.Exp(-(a - s) * t);
        var fast = (s - a) / (2 * s) * Math.Exp(-(a + s) * t);
        return 1 - (slow + fast);
    }

    /// <inheritdoc />
    public override string ToString() => CssValue;
}
