namespace BlazorMotion.Tests;

public class MotionAnimationTests
{
    private static readonly MotionTransition _seedTransition = MotionTransition.Default with
    {
        Duration = TimeSpan.FromMilliseconds(500),
        Delay = TimeSpan.FromMilliseconds(100),
        Repeat = 2,
        Direction = MotionDirection.Alternate,
    };

    private static MotionKeyframe Frame(string opacity, double? offset = null) =>
        new(new Dictionary<string, string> { ["opacity"] = opacity }, offset);

    private static MotionAnimation Animate(MotionTransition? transition = null, bool fillForwards = true) =>
        new(new List<MotionKeyframe> { Frame("0"), Frame("1") }, transition ?? _seedTransition, fillForwards);

    private static void AssertSameProperties(
        IReadOnlyDictionary<string, string> expected,
        IReadOnlyDictionary<string, string> actual)
    {
        Assert.Equal(expected.Count, actual.Count);
        foreach (var (key, value) in expected)
        {
            Assert.True(actual.TryGetValue(key, out var actualValue), $"Missing property '{key}'.");
            Assert.Equal(value, actualValue);
        }
    }

    private static void AssertSameKeyframes(
        IReadOnlyList<MotionKeyframe> expected,
        IReadOnlyList<MotionKeyframe> actual)
    {
        Assert.Equal(expected.Count, actual.Count);
        for (var i = 0; i < expected.Count; i++)
        {
            Assert.Equal(expected[i].Offset, actual[i].Offset);
            AssertSameProperties(expected[i].Properties, actual[i].Properties);
        }
    }

    [Fact]
    public void WithTransition_ReturnsNewInstance_WithNewTransition_AndSameKeyframes()
    {
        var original = Animate();
        var transition = MotionTransition.Default with { Duration = TimeSpan.FromSeconds(2) };

        var result = original.WithTransition(transition);

        Assert.NotSame(original, result);
        Assert.Equal(transition, result.Transition);
        Assert.Same(original.Keyframes, result.Keyframes);
    }

    [Fact]
    public void WithTransition_Null_ThrowsArgumentNullException()
    {
        var animation = Animate();

        Assert.Throws<ArgumentNullException>(() => animation.WithTransition(null!));
    }

    [Fact]
    public void WithDelay_ChangesOnlyDelay()
    {
        var original = Animate(fillForwards: false);
        var delay = TimeSpan.FromSeconds(3);

        var result = original.WithDelay(delay);

        Assert.Equal(original.Transition with { Delay = delay }, result.Transition);
        Assert.Same(original.Keyframes, result.Keyframes);
        Assert.Equal(original.FillForwards, result.FillForwards);
    }

    [Fact]
    public void WithDelay_Negative_ThrowsArgumentOutOfRangeException()
    {
        var animation = Animate();

        Assert.Throws<ArgumentOutOfRangeException>(() => animation.WithDelay(TimeSpan.FromMilliseconds(-1)));
    }

    [Fact]
    public void WithDuration_ChangesOnlyDuration()
    {
        var original = Animate(fillForwards: false);
        var duration = TimeSpan.FromSeconds(4);

        var result = original.WithDuration(duration);

        Assert.Equal(original.Transition with { Duration = duration }, result.Transition);
        Assert.Same(original.Keyframes, result.Keyframes);
        Assert.Equal(original.FillForwards, result.FillForwards);
    }

    [Fact]
    public void WithDuration_Negative_ThrowsArgumentOutOfRangeException()
    {
        var animation = Animate();

        Assert.Throws<ArgumentOutOfRangeException>(() => animation.WithDuration(TimeSpan.FromMilliseconds(-1)));
    }

    [Fact]
    public void Reversed_ReversesOrder_AndMirrorsExplicitOffsets()
    {
        var animation = new MotionAnimation(
            new List<MotionKeyframe>
            {
                Frame("A", 0),
                Frame("B", 0.25),
                Frame("C", null),
                Frame("D", 1),
            },
            MotionTransition.Default);

        var reversed = animation.Reversed();

        Assert.Equal(4, reversed.Keyframes.Count);
        Assert.Equal(0, reversed.Keyframes[0].Offset);
        Assert.Null(reversed.Keyframes[1].Offset);
        Assert.Equal(0.75, reversed.Keyframes[2].Offset);
        Assert.Equal(1, reversed.Keyframes[3].Offset);

        AssertSameProperties(new Dictionary<string, string> { ["opacity"] = "D" }, reversed.Keyframes[0].Properties);
        AssertSameProperties(new Dictionary<string, string> { ["opacity"] = "C" }, reversed.Keyframes[1].Properties);
        AssertSameProperties(new Dictionary<string, string> { ["opacity"] = "B" }, reversed.Keyframes[2].Properties);
        AssertSameProperties(new Dictionary<string, string> { ["opacity"] = "A" }, reversed.Keyframes[3].Properties);
    }

    [Fact]
    public void Reversed_Twice_RestoresOriginalKeyframeSequence()
    {
        var animation = new MotionAnimation(
            new List<MotionKeyframe>
            {
                Frame("A", 0),
                Frame("B", 0.25),
                Frame("C", null),
                Frame("D", 1),
            },
            MotionTransition.Default);

        var roundTrip = animation.Reversed().Reversed();

        AssertSameKeyframes(animation.Keyframes, roundTrip.Keyframes);
    }

    [Fact]
    public void Reversed_KeepsTransitionAndFillForwards()
    {
        var animation = Animate(fillForwards: false);

        var reversed = animation.Reversed();

        Assert.Equal(animation.Transition, reversed.Transition);
        Assert.False(reversed.FillForwards);
    }

    [Fact]
    public void FromKeyframes_EmptySequence_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => MotionAnimation.FromKeyframes(Array.Empty<MotionKeyframe>()));
    }

    [Fact]
    public void FromKeyframes_NullTransition_UsesDefault()
    {
        var result = MotionAnimation.FromKeyframes(new[] { Frame("0"), Frame("1") });

        Assert.Same(MotionTransition.Default, result.Transition);
    }

    [Fact]
    public void FromKeyframes_ReturnedKeyframes_NotAffectedByLaterMutationOfSource()
    {
        var source = new List<MotionKeyframe> { Frame("0"), Frame("1") };

        var result = MotionAnimation.FromKeyframes(source);
        source.Add(Frame("2"));
        source[0] = Frame("changed");

        Assert.Equal(2, result.Keyframes.Count);
        Assert.Equal("0", result.Keyframes[0].Properties["opacity"]);
        Assert.Equal("1", result.Keyframes[1].Properties["opacity"]);
    }

    [Fact]
    public void RecordEquality_UsesReferenceForLists()
    {
        // Documents current behaviour: the generated Equals compares Keyframes with the list's
        // reference equality, not element-wise. Two animations are equal only when they share the
        // same keyframe list instance. Do not rely on deep equality of keyframes here.
        var sharedKeyframes = new List<MotionKeyframe> { Frame("0"), Frame("1") };

        var first = new MotionAnimation(sharedKeyframes, _seedTransition);
        var second = new MotionAnimation(sharedKeyframes, _seedTransition);
        Assert.Equal(first, second);

        var sameContentDifferentList = new MotionAnimation(
            new List<MotionKeyframe> { Frame("0"), Frame("1") },
            _seedTransition);
        Assert.NotEqual(first, sameContentDifferentList);
    }
}
