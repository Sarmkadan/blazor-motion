using Microsoft.AspNetCore.Components;

namespace BlazorMotion;

/// <summary>
/// One entry in a <see cref="MotionTimeline"/>: an element, an animation and when it starts relative to the timeline origin.
/// </summary>
/// <param name="Target">Element to animate.</param>
/// <param name="Animation">Animation to run.</param>
/// <param name="StartAt">Offset from the timeline start.</param>
public sealed record MotionTimelineEntry(
    ElementReference Target,
    MotionAnimation Animation,
    TimeSpan StartAt);

/// <summary>
/// Orchestrates several animations across several elements. Supports sequential placement,
/// explicit offsets and staggering, similar to GSAP timelines or Framer Motion's <c>staggerChildren</c>.
/// </summary>
public sealed class MotionTimeline
{
    private readonly List<MotionTimelineEntry> _entries = [];

    /// <summary>The entries added so far, in insertion order.</summary>
    public IReadOnlyList<MotionTimelineEntry> Entries => _entries.AsReadOnly();

    /// <summary>True when any entry repeats forever, so it has no finite end time.</summary>
    public bool HasInfiniteEntry =>
        _entries.Any(e => e.Animation.Transition.Repeat == MotionTransition.InfiniteRepeat);

    /// <summary>Total duration including the longest-running entry and its delay/repeat.</summary>
    public TimeSpan TotalDuration => throw new NotImplementedException();

    /// <summary>
    /// Appends an animation. By default it starts when the previous entry ends.
    /// </summary>
    /// <param name="target">Element to animate.</param>
    /// <param name="animation">Animation to run.</param>
    /// <param name="offset">Shift relative to the default start; negative overlaps the previous entry.</param>
    /// <returns>This timeline.</returns>
    public MotionTimeline Add(ElementReference target, MotionAnimation animation, TimeSpan? offset = null)
    {
        ArgumentNullException.ThrowIfNull(animation);

        var defaultStart = _entries.Count == 0 ? TimeSpan.Zero : EntryEnd(_entries[^1]);
        var start = defaultStart + (offset ?? TimeSpan.Zero);

        return AddEntry(new MotionTimelineEntry(target, animation, start < TimeSpan.Zero ? TimeSpan.Zero : start));
    }

    /// <summary>
    /// Appends an animation starting at an absolute position in the timeline.
    /// </summary>
    /// <param name="target">Element to animate.</param>
    /// <param name="animation">Animation to run.</param>
    /// <param name="at">Absolute offset from the timeline start.</param>
    /// <returns>This timeline.</returns>
    public MotionTimeline AddAt(ElementReference target, MotionAnimation animation, TimeSpan at)
    {
        ArgumentNullException.ThrowIfNull(animation);

        return AddEntry(new MotionTimelineEntry(target, animation, at));
    }

    private MotionTimeline AddEntry(MotionTimelineEntry entry)
    {
        _entries.Add(entry);
        return this;
    }

    /// <summary>
    /// End time of an entry: start + delay + one duration per iteration. An infinite repeat counts as a single
    /// iteration, since it has no finite end.
    /// </summary>
    private static TimeSpan EntryEnd(MotionTimelineEntry e)
    {
        var transition = e.Animation.Transition;
        var iterations = transition.Repeat == MotionTransition.InfiniteRepeat ? 0 : Math.Max(transition.Repeat, 0);

        return e.StartAt + transition.Delay + transition.Duration * (1 + iterations);
    }

    /// <summary>
    /// Adds the same animation to many elements, each starting <paramref name="stagger"/> after the previous one.
    /// </summary>
    /// <param name="targets">Elements in stagger order.</param>
    /// <param name="animation">Animation applied to each.</param>
    /// <param name="stagger">Gap between consecutive starts.</param>
    /// <returns>This timeline.</returns>
    public MotionTimeline Stagger(IEnumerable<ElementReference> targets, MotionAnimation animation, TimeSpan stagger) => throw new NotImplementedException();
}
