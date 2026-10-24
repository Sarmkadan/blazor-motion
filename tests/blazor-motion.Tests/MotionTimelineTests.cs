using Microsoft.AspNetCore.Components;

namespace BlazorMotion.Tests;

public class MotionTimelineTests
{
    [Fact]
    public void NewTimeline_IsEmpty()
    {
        var timeline = new MotionTimeline();

        Assert.Empty(timeline.Entries);
        Assert.Equal(TimeSpan.Zero, timeline.TotalDuration);
    }

    [Fact]
    public void Add_FirstEntry_StartsAtZeroAndSetsTotalDuration()
    {
        var timeline = new MotionTimeline();

        timeline.Add(Element(), Anim(300));

        Assert.Equal(TimeSpan.Zero, Assert.Single(timeline.Entries).StartAt);
        Assert.Equal(TimeSpan.FromMilliseconds(300), timeline.TotalDuration);
    }

    [Fact]
    public void Add_SecondEntry_StartsWhenPreviousEnds()
    {
        var timeline = new MotionTimeline();
        timeline.Add(Element(), Anim(300));

        timeline.Add(Element(), Anim(100));

        Assert.Equal(TimeSpan.FromMilliseconds(300), timeline.Entries[1].StartAt);
        Assert.Equal(TimeSpan.FromMilliseconds(400), timeline.TotalDuration);
    }

    [Fact]
    public void Add_NegativeOffset_OverlapsPreviousEntry()
    {
        var timeline = new MotionTimeline();
        timeline.Add(Element(), Anim(300));

        timeline.Add(Element(), Anim(100), offset: TimeSpan.FromMilliseconds(-100));

        Assert.Equal(TimeSpan.FromMilliseconds(200), timeline.Entries[1].StartAt);
    }

    [Fact]
    public void Add_NegativeOffsetBeyondPreviousEnd_ClampsStartToZero()
    {
        var timeline = new MotionTimeline();
        timeline.Add(Element(), Anim(300));

        timeline.Add(Element(), Anim(100), offset: TimeSpan.FromMilliseconds(-500));

        Assert.Equal(TimeSpan.Zero, timeline.Entries[1].StartAt);
    }

    [Fact]
    public void AddAt_StartsAtAbsolutePosition_IgnoringPreviousEntries()
    {
        var timeline = new MotionTimeline();
        timeline.Add(Element(), Anim(300));

        timeline.AddAt(Element(), Anim(200), TimeSpan.FromSeconds(1));

        Assert.Equal(TimeSpan.FromSeconds(1), timeline.Entries[1].StartAt);
    }

    [Fact]
    public void Add_AfterAddAt_StartsWhenAddAtEntryEnds()
    {
        var timeline = new MotionTimeline();
        timeline.Add(Element(), Anim(300));
        timeline.AddAt(Element(), Anim(200), TimeSpan.FromSeconds(1));

        timeline.Add(Element(), Anim(100));

        Assert.Equal(TimeSpan.FromSeconds(1) + TimeSpan.FromMilliseconds(200), timeline.Entries[2].StartAt);
        Assert.Equal(TimeSpan.FromSeconds(1) + TimeSpan.FromMilliseconds(300), timeline.TotalDuration);
    }

    [Fact]
    public void Add_PreviousEntryHasDelay_DelayCountsTowardEnd()
    {
        var timeline = new MotionTimeline();
        timeline.Add(Element(), Anim(300, delayMs: 200));

        timeline.Add(Element(), Anim(100));

        Assert.Equal(TimeSpan.FromMilliseconds(500), timeline.Entries[1].StartAt);
        Assert.Equal(TimeSpan.FromMilliseconds(600), timeline.TotalDuration);
    }

    [Fact]
    public void Add_PreviousEntryRepeatsTwice_EndsAtThreeTimesDuration()
    {
        var timeline = new MotionTimeline();
        timeline.Add(Element(), Anim(300, repeat: 2));

        timeline.Add(Element(), Anim(100));

        Assert.Equal(TimeSpan.FromMilliseconds(900), timeline.Entries[1].StartAt);
        Assert.Equal(TimeSpan.FromMilliseconds(1000), timeline.TotalDuration);
        Assert.False(timeline.HasInfiniteEntry);
    }

    [Fact]
    public void Add_InfiniteRepeat_CountsAsSingleIterationAndSetsHasInfiniteEntry()
    {
        var timeline = new MotionTimeline();

        timeline.Add(Element(), Anim(300, repeat: int.MaxValue));

        Assert.True(timeline.HasInfiniteEntry);
        Assert.Equal(TimeSpan.FromMilliseconds(300), timeline.TotalDuration);
    }

    [Fact]
    public void Add_AfterInfiniteRepeat_StartsAfterSingleIteration()
    {
        var timeline = new MotionTimeline();
        timeline.Add(Element(), Anim(300, repeat: int.MaxValue));

        timeline.Add(Element(), Anim(100));

        Assert.Equal(TimeSpan.FromMilliseconds(300), timeline.Entries[1].StartAt);
    }

    [Fact]
    public void Stagger_AfterExistingEntry_StartsEachTargetAtStaggerSteps()
    {
        var timeline = new MotionTimeline();
        timeline.Add(Element(), Anim(300));
        var targets = new[] { Element(), Element(), Element() };

        timeline.Stagger(targets, Anim(200), TimeSpan.FromMilliseconds(80));

        var added = timeline.Entries.Skip(1).ToList();
        Assert.Equal(
            new[] { TimeSpan.FromMilliseconds(300), TimeSpan.FromMilliseconds(380), TimeSpan.FromMilliseconds(460) },
            added.Select(e => e.StartAt));
        Assert.Equal(targets.Select(t => t.Id), added.Select(e => e.Target.Id));
        Assert.Equal(TimeSpan.FromMilliseconds(660), timeline.TotalDuration);
    }

    [Fact]
    public void Stagger_EmptyTargets_AddsNothing()
    {
        var timeline = new MotionTimeline();
        timeline.Add(Element(), Anim(300));

        timeline.Stagger(Array.Empty<ElementReference>(), Anim(200), TimeSpan.FromMilliseconds(80));

        Assert.Single(timeline.Entries);
        Assert.Equal(TimeSpan.FromMilliseconds(300), timeline.TotalDuration);
    }

    [Fact]
    public void Entries_KeepInsertionOrderAndTargets()
    {
        var timeline = new MotionTimeline();
        var first = Element();
        var second = Element();
        var third = Element();

        timeline.Add(first, Anim(100));
        timeline.AddAt(third, Anim(100), TimeSpan.FromMilliseconds(50));
        timeline.Add(second, Anim(100));

        Assert.Equal(
            new[] { first.Id, third.Id, second.Id },
            timeline.Entries.Select(e => e.Target.Id));
    }

    [Fact]
    public void Clear_RemovesAllEntries()
    {
        var timeline = new MotionTimeline();
        timeline.Add(Element(), Anim(300));
        timeline.AddAt(Element(), Anim(300, repeat: int.MaxValue), TimeSpan.FromSeconds(1));

        timeline.Clear();

        Assert.Empty(timeline.Entries);
        Assert.Equal(TimeSpan.Zero, timeline.TotalDuration);
        Assert.False(timeline.HasInfiniteEntry);
    }

    [Fact]
    public void Clear_ThenAdd_StartsAtZero()
    {
        var timeline = new MotionTimeline();
        timeline.Add(Element(), Anim(300));
        timeline.Clear();

        timeline.Add(Element(), Anim(100));

        Assert.Equal(TimeSpan.Zero, Assert.Single(timeline.Entries).StartAt);
    }

    [Fact]
    public void FluentMethods_ReturnSameInstance()
    {
        var timeline = new MotionTimeline();

        Assert.Same(timeline, timeline.Add(Element(), Anim(100)));
        Assert.Same(timeline, timeline.AddAt(Element(), Anim(100), TimeSpan.FromSeconds(1)));
        Assert.Same(timeline, timeline.Stagger(new[] { Element() }, Anim(100), TimeSpan.FromMilliseconds(10)));
        Assert.Same(timeline, timeline.Stagger(new[] { Element() }, Anim(100), TimeSpan.FromMilliseconds(10), TimeSpan.FromSeconds(2)));
    }

    private static ElementReference Element() => new(Guid.NewGuid().ToString());

    private static MotionAnimation Anim(int durationMs, int delayMs = 0, int repeat = 0) =>
        new(
            [new MotionKeyframe(new Dictionary<string, string> { ["opacity"] = "0" })],
            MotionTransition.Default with
            {
                Duration = TimeSpan.FromMilliseconds(durationMs),
                Delay = TimeSpan.FromMilliseconds(delayMs),
                Repeat = repeat,
            });
}
