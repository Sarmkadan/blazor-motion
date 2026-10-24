using System.Text.Json;
using BlazorMotion.Interop;
using Microsoft.AspNetCore.Components;

namespace BlazorMotion.Tests;

public class MotionPayloadTests
{
    [Fact]
    public void ToPayload_Duration_ConvertsTimeSpanToMilliseconds()
    {
        var animation = Anim(Transition(duration: TimeSpan.FromMilliseconds(600)));

        var payload = MotionPayloadMapper.ToPayload(animation);

        Assert.Equal(600d, payload.Timing.Duration);
    }

    [Fact]
    public void ToPayload_Delay_ConvertsTimeSpanToMilliseconds()
    {
        var animation = Anim(Transition(delay: TimeSpan.FromMilliseconds(150)));

        var payload = MotionPayloadMapper.ToPayload(animation);

        Assert.Equal(150d, payload.Timing.Delay);
    }

    [Fact]
    public void ToPayload_RepeatZero_PlaysOnce()
    {
        var payload = MotionPayloadMapper.ToPayload(Anim(Transition(repeat: 0)));

        Assert.Equal(1d, payload.Timing.Iterations);
    }

    [Fact]
    public void ToPayload_RepeatTwo_PlaysThreeTimes()
    {
        var payload = MotionPayloadMapper.ToPayload(Anim(Transition(repeat: 2)));

        Assert.Equal(3d, payload.Timing.Iterations);
    }

    [Fact]
    public void ToPayload_InfiniteRepeat_MapsToNullIterations()
    {
        var payload = MotionPayloadMapper.ToPayload(Anim(Transition(repeat: MotionTransition.InfiniteRepeat)));

        Assert.Null(payload.Timing.Iterations);
    }

    [Theory]
    [InlineData(MotionDirection.Normal, "normal")]
    [InlineData(MotionDirection.Reverse, "reverse")]
    [InlineData(MotionDirection.Alternate, "alternate")]
    public void ToPayload_Direction_MapsToWaapiString(MotionDirection direction, string expected)
    {
        var payload = MotionPayloadMapper.ToPayload(Anim(Transition(direction: direction)));

        Assert.Equal(expected, payload.Timing.Direction);
    }

    [Theory]
    [InlineData(true, "forwards")]
    [InlineData(false, "none")]
    public void ToPayload_FillForwards_MapsToWaapiFillMode(bool fillForwards, string expected)
    {
        var payload = MotionPayloadMapper.ToPayload(Anim(Transition(), fillForwards));

        Assert.Equal(expected, payload.Timing.Fill);
    }

    [Fact]
    public void ToPayload_NamedEasing_PassesCssValueThrough()
    {
        var payload = MotionPayloadMapper.ToPayload(Anim(Transition(easing: MotionEasing.EaseInOut)));

        Assert.Equal("ease-in-out", payload.Timing.Easing);
    }

    [Fact]
    public void ToPayload_CubicBezierEasing_PassesCssValueThrough()
    {
        var easing = MotionEasing.CubicBezier(0.4, 0, 0.2, 1);

        var payload = MotionPayloadMapper.ToPayload(Anim(Transition(easing: easing)));

        Assert.Equal(easing.CssValue, payload.Timing.Easing);
        Assert.Equal("cubic-bezier(0.4, 0, 0.2, 1)", payload.Timing.Easing);
    }

    [Fact]
    public void ToPayload_Keyframes_CopiesPropertiesByContentAndPreservesOffset()
    {
        var source = new Dictionary<string, string> { ["opacity"] = "0", ["transform"] = "translateX(0px)" };
        var animation = MotionAnimation.FromKeyframes(
        [
            new MotionKeyframe(source, 0.25),
            new MotionKeyframe(new Dictionary<string, string> { ["opacity"] = "1" }, null),
        ]);

        var payload = MotionPayloadMapper.ToPayload(animation);

        Assert.Equal(2, payload.Keyframes.Count);

        var first = payload.Keyframes[0];
        Assert.Equal(0.25, first.Offset);
        Assert.Equal(new Dictionary<string, string> { ["opacity"] = "0", ["transform"] = "translateX(0px)" }, first.Properties);

        Assert.Null(payload.Keyframes[1].Offset);
        Assert.Equal(new Dictionary<string, string> { ["opacity"] = "1" }, payload.Keyframes[1].Properties);
    }

    [Fact]
    public void ToPayload_Keyframes_AreIndependentOfSourceDictionary()
    {
        var source = new Dictionary<string, string> { ["opacity"] = "0" };
        var animation = MotionAnimation.FromKeyframes([new MotionKeyframe(source)]);

        var payload = MotionPayloadMapper.ToPayload(animation);
        source["opacity"] = "changed";
        source["transform"] = "scale(2)";

        Assert.Equal(new Dictionary<string, string> { ["opacity"] = "0" }, payload.Keyframes[0].Properties);
    }

    [Fact]
    public void ToPayload_Timeline_YieldsOneEntryPerTimelineEntry()
    {
        var first = Element();
        var second = Element();
        var timeline = new MotionTimeline()
            .Add(first, Anim(Transition(duration: TimeSpan.FromMilliseconds(300))))
            .AddAt(second, Anim(Transition(duration: TimeSpan.FromMilliseconds(100))), TimeSpan.FromMilliseconds(250));

        var entries = MotionPayloadMapper.ToPayload(timeline);

        Assert.Equal(2, entries.Count);
        Assert.Equal(first, entries[0].Target);
        Assert.Equal(0d, entries[0].StartAt);
        Assert.Equal(second, entries[1].Target);
        Assert.Equal(250d, entries[1].StartAt);
        Assert.Equal(100d, entries[1].Animation.Timing.Duration);
    }

    [Fact]
    public void ToPayload_EmptyTimeline_YieldsNoEntries()
    {
        var entries = MotionPayloadMapper.ToPayload(new MotionTimeline());

        Assert.Empty(entries);
    }

    [Fact]
    public void AnimationPayload_Serialises_WithCamelCaseKeysAndNoNonFiniteNumbers()
    {
        var animation = Anim(Transition(
            duration: TimeSpan.FromMilliseconds(600),
            easing: MotionEasing.CubicBezier(0.4, 0, 0.2, 1)));
        var payload = MotionPayloadMapper.ToPayload(animation);

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.DoesNotContain("NaN", json);
        Assert.DoesNotContain("Infinity", json);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        Assert.True(root.TryGetProperty("keyframes", out var keyframes));
        Assert.Equal(JsonValueKind.Array, keyframes.ValueKind);
        Assert.True(root.TryGetProperty("timing", out var timing));

        Assert.Equal(600d, timing.GetProperty("duration").GetDouble());
        Assert.Equal("cubic-bezier(0.4, 0, 0.2, 1)", timing.GetProperty("easing").GetString());
    }

    [Fact]
    public void AnimationPayload_InfiniteRepeat_SerialisesIterationsAsNull()
    {
        var payload = MotionPayloadMapper.ToPayload(Anim(Transition(repeat: MotionTransition.InfiniteRepeat)));

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        using var document = JsonDocument.Parse(json);
        var iterations = document.RootElement.GetProperty("timing").GetProperty("iterations");
        Assert.Equal(JsonValueKind.Null, iterations.ValueKind);
        Assert.DoesNotContain("Infinity", json);
    }

    private static MotionTransition Transition(
        TimeSpan? duration = null,
        TimeSpan? delay = null,
        MotionEasing? easing = null,
        int repeat = 0,
        MotionDirection direction = MotionDirection.Normal)
    {
        return MotionTransition.Default with
        {
            Duration = duration ?? MotionTransition.Default.Duration,
            Delay = delay ?? MotionTransition.Default.Delay,
            Easing = easing ?? MotionTransition.Default.Easing,
            Repeat = repeat,
            Direction = direction,
        };
    }

    private static MotionAnimation Anim(MotionTransition transition, bool fillForwards = true) =>
        new(
            [
                new MotionKeyframe(new Dictionary<string, string> { ["opacity"] = "0" }, 0),
                new MotionKeyframe(new Dictionary<string, string> { ["opacity"] = "1" }, 1),
            ],
            transition,
            fillForwards);

    private static ElementReference Element() => new(Guid.NewGuid().ToString());
}
