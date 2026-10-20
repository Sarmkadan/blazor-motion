using Microsoft.AspNetCore.Components;

namespace BlazorMotion.Interop;

/// <summary>
/// Serialisable representation of a single keyframe for the JS interop layer.
/// </summary>
/// <param name="Properties">CSS property name (camelCase) to value.</param>
/// <param name="Offset">Position in the animation (0..1), or <c>null</c> for even distribution.</param>
internal sealed record KeyframePayload(
    Dictionary<string, string> Properties,
    double? Offset);

/// <summary>
/// Serialisable timing parameters for the JS interop layer.
/// </summary>
/// <param name="Duration">Duration in milliseconds.</param>
/// <param name="Delay">Delay in milliseconds.</param>
/// <param name="Easing">CSS easing string.</param>
/// <param name="Iterations">
/// Number of iterations. <c>null</c> means infinite (the JS layer should interpret this
/// as <c>Infinity</c>). A finite value of 1 means play once, 2 means play twice, etc.
/// </param>
/// <param name="Direction">WAAPI direction string: <c>normal</c>, <c>reverse</c>, or <c>alternate</c>.</param>
/// <param name="Fill">WAAPI fill mode: <c>forwards</c> or <c>none</c>.</param>
internal sealed record TimingPayload(
    double Duration,
    double Delay,
    string Easing,
    double? Iterations,
    string Direction,
    string Fill);

/// <summary>
/// Serialisable animation definition combining keyframes and timing.
/// </summary>
/// <param name="Keyframes">Ordered keyframes.</param>
/// <param name="Timing">Timing parameters.</param>
internal sealed record AnimationPayload(
    IReadOnlyList<KeyframePayload> Keyframes,
    TimingPayload Timing);

/// <summary>
/// Serialisable timeline entry: a target element, its animation and when it starts.
/// </summary>
/// <param name="Target">Element to animate.</param>
/// <param name="Animation">Animation definition.</param>
/// <param name="StartAt">Offset from the timeline start, in milliseconds.</param>
internal sealed record TimelineEntryPayload(
    ElementReference Target,
    AnimationPayload Animation,
    double StartAt);

/// <summary>
/// Maps the public C# animation model to the internal serialisable DTOs sent to <c>blazor-motion.js</c>.
/// </summary>
internal static class MotionPayloadMapper
{
    /// <summary>
    /// Converts a <see cref="MotionAnimation"/> to an <see cref="AnimationPayload"/>.
    /// </summary>
    /// <param name="animation">The animation to convert.</param>
    /// <returns>A serialisable payload.</returns>
    public static AnimationPayload ToPayload(MotionAnimation animation)
    {
        ArgumentNullException.ThrowIfNull(animation);

        var keyframes = animation.Keyframes
            .Select(k => new KeyframePayload(
                new Dictionary<string, string>(k.Properties),
                k.Offset))
            .ToList();

        var timing = MapTiming(animation.Transition, animation.FillForwards);

        return new AnimationPayload(keyframes, timing);
    }

    /// <summary>
    /// Converts a <see cref="MotionTimeline"/> to a list of <see cref="TimelineEntryPayload"/>.
    /// </summary>
    /// <param name="timeline">The timeline to convert.</param>
    /// <returns>Serialisable entries.</returns>
    public static IReadOnlyList<TimelineEntryPayload> ToPayload(MotionTimeline timeline)
    {
        ArgumentNullException.ThrowIfNull(timeline);

        return timeline.Entries
            .Select(e => new TimelineEntryPayload(
                e.Target,
                ToPayload(e.Animation),
                e.StartAt.TotalMilliseconds))
            .ToList();
    }

    private static TimingPayload MapTiming(MotionTransition transition, bool fillForwards)
    {
        // Repeat 0 -> play once -> iterations 1
        // Repeat n -> play n+1 times -> iterations n+1
        // Repeat int.MaxValue -> infinite -> iterations null (JS interprets as Infinity)
        double? iterations = transition.Repeat == int.MaxValue
            ? null
            : transition.Repeat + 1.0;

        string direction = transition.Direction switch
        {
            MotionDirection.Reverse => "reverse",
            MotionDirection.Alternate => "alternate",
            _ => "normal",
        };

        string fill = fillForwards ? "forwards" : "none";

        return new TimingPayload(
            Duration: transition.Duration.TotalMilliseconds,
            Delay: transition.Delay.TotalMilliseconds,
            Easing: transition.Easing.CssValue,
            Iterations: iterations,
            Direction: direction,
            Fill: fill);
    }
}
