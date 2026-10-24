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
    public TimeSpan TotalDuration => _entries.Count == 0 ? TimeSpan.Zero : _entries.Max(EntryEnd);

    /// <summary>
    /// Appends an animation. By default it starts when the previous entry ends.
    /// </summary>
    /// <param name="target">Element to animate.</param>
    /// <param name="animation">Animation to run.</param>
    /// <param name="offset">Shift relative to the default start; negative overlaps the previous entry.</param>
    /// <returns>This timeline.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="animation"/> is <see langword="null"/>.</exception>
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
    /// <param name="at">Absolute offset from the timeline start. Must be non-negative.</param>
    /// <returns>This timeline.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="animation"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="at"/> is negative.</exception>
    public MotionTimeline AddAt(ElementReference target, MotionAnimation animation, TimeSpan at)
    {
        ArgumentNullException.ThrowIfNull(animation);
        if (at < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(at), at, "Timeline offset must be non-negative.");
        }

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
    /// The first element starts when the last existing entry ends (or at zero for an empty timeline).
    /// </summary>
    /// <param name="targets">Elements in stagger order.</param>
    /// <param name="animation">Animation applied to each.</param>
    /// <param name="stagger">Gap between consecutive starts. Must be non-negative.</param>
    /// <returns>This timeline.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="targets"/> or <paramref name="animation"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="stagger"/> is negative.</exception>
    public MotionTimeline Stagger(IEnumerable<ElementReference> targets, MotionAnimation animation, TimeSpan stagger)
    {
        ArgumentNullException.ThrowIfNull(targets);
        ArgumentNullException.ThrowIfNull(animation);

        var baseStart = _entries.Count == 0 ? TimeSpan.Zero : EntryEnd(_entries[^1]);

        return StaggerFrom(targets, animation, stagger, baseStart);
    }

    /// <summary>
    /// Adds the same animation to many elements, each starting <paramref name="stagger"/> after the previous one,
    /// with the first element starting at the absolute position <paramref name="startAt"/>.
    /// </summary>
    /// <param name="targets">Elements in stagger order.</param>
    /// <param name="animation">Animation applied to each.</param>
    /// <param name="stagger">Gap between consecutive starts. Must be non-negative.</param>
    /// <param name="startAt">Absolute offset from the timeline start of the first element. Must be non-negative.</param>
    /// <returns>This timeline.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="targets"/> or <paramref name="animation"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="stagger"/> or <paramref name="startAt"/> is negative.</exception>
    public MotionTimeline Stagger(IEnumerable<ElementReference> targets, MotionAnimation animation, TimeSpan stagger, TimeSpan startAt)
    {
        if (startAt < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(startAt), startAt, "Timeline offset must be non-negative.");
        }

        return StaggerFrom(targets, animation, stagger, startAt);
    }

    /// <summary>
    /// Removes all entries, returning the timeline to its empty state.
    /// </summary>
    public void Clear() => _entries.Clear();

    private MotionTimeline StaggerFrom(IEnumerable<ElementReference> targets, MotionAnimation animation, TimeSpan stagger, TimeSpan baseStart)
    {
        ArgumentNullException.ThrowIfNull(targets);
        ArgumentNullException.ThrowIfNull(animation);
        if (stagger < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(stagger), stagger, "Stagger gap must be non-negative.");
        }

        var list = targets.ToList();
        for (var i = 0; i < list.Count; i++)
        {
            AddEntry(new MotionTimelineEntry(list[i], animation, baseStart + stagger * i));
        }

        return this;
    }
}
