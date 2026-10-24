using System.Globalization;

namespace BlazorMotion.Tests;

public class MotionBuilderTests
{
    [Fact]
    public void Build_WithoutTo_Throws()
    {
        var builder = new MotionBuilder().From(opacity: 0);

        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void To_X_YieldsSingleTranslateKeyframe()
    {
        var animation = new MotionBuilder().To(x: 100).Build();

        var keyframe = Assert.Single(animation.Keyframes);
        Assert.Equal("translateX(100px)", keyframe.Properties["transform"]);
        Assert.Single(keyframe.Properties);
        Assert.Null(keyframe.Offset);
    }

    [Fact]
    public void To_AllProperties_EmitsTransformAndOpacity()
    {
        var animation = new MotionBuilder()
            .To(x: 10, y: -5, scale: 1.5, rotate: 90, opacity: 0.5)
            .Build();

        var keyframe = Assert.Single(animation.Keyframes);
        Assert.Equal(
            "translateX(10px) translateY(-5px) scale(1.5) rotate(90deg)",
            keyframe.Properties["transform"]);
        Assert.Equal("0.5", keyframe.Properties["opacity"]);
        Assert.Equal(2, keyframe.Properties.Count);
    }

    [Fact]
    public void From_ThenTo_YieldsTwoKeyframesWithFromFirstAtOffsetZero()
    {
        var animation = new MotionBuilder()
            .From(opacity: 0)
            .To(opacity: 1)
            .Build();

        Assert.Equal(2, animation.Keyframes.Count);

        var from = animation.Keyframes[0];
        Assert.Equal(0, from.Offset);
        Assert.Equal("0", from.Properties["opacity"]);

        Assert.Equal("1", animation.Keyframes[1].Properties["opacity"]);
    }

    [Fact]
    public void From_CalledTwice_KeepsOnlyLast()
    {
        var animation = new MotionBuilder()
            .From(opacity: 0)
            .From(opacity: 0.25)
            .To(opacity: 1)
            .Build();

        Assert.Equal(2, animation.Keyframes.Count);
        Assert.Equal("0.25", animation.Keyframes[0].Properties["opacity"]);
    }

    [Fact]
    public void ThreeTo_YieldsThreeKeyframesInOrder()
    {
        var animation = new MotionBuilder()
            .To(opacity: 0.2)
            .To(opacity: 0.6)
            .To(opacity: 1)
            .Build();

        Assert.Equal(3, animation.Keyframes.Count);
        Assert.Equal("0.2", animation.Keyframes[0].Properties["opacity"]);
        Assert.Equal("0.6", animation.Keyframes[1].Properties["opacity"]);
        Assert.Equal("1", animation.Keyframes[2].Properties["opacity"]);
    }

    [Fact]
    public void From_ThreeTo_YieldsFourKeyframesWithFromFirst()
    {
        var animation = new MotionBuilder()
            .From(opacity: 0)
            .To(opacity: 0.2)
            .To(opacity: 0.6)
            .To(opacity: 1)
            .Build();

        Assert.Equal(4, animation.Keyframes.Count);
        Assert.Equal("0", animation.Keyframes[0].Properties["opacity"]);
        Assert.Equal("0.2", animation.Keyframes[1].Properties["opacity"]);
        Assert.Equal("0.6", animation.Keyframes[2].Properties["opacity"]);
        Assert.Equal("1", animation.Keyframes[3].Properties["opacity"]);
    }

    [Fact]
    public void DictionaryOverload_CopiesDictionary()
    {
        var css = new Dictionary<string, string> { ["backgroundColor"] = "red" };
        var builder = new MotionBuilder().To(css, 0.5);

        css["backgroundColor"] = "blue";
        css["color"] = "white";

        var keyframe = Assert.Single(builder.Build().Keyframes);
        Assert.Equal("red", keyframe.Properties["backgroundColor"]);
        Assert.False(keyframe.Properties.ContainsKey("color"));
    }

    [Fact]
    public void DictionaryOverload_StoresOffset()
    {
        var animation = new MotionBuilder()
            .To(new Dictionary<string, string> { ["backgroundColor"] = "red" }, 0.5)
            .Build();

        Assert.Equal(0.5, Assert.Single(animation.Keyframes).Offset);
    }

    [Fact]
    public void DictionaryOverload_DecreasingOffsets_BuildThrows()
    {
        var builder = new MotionBuilder()
            .To(new Dictionary<string, string> { ["backgroundColor"] = "red" }, 0.5)
            .To(new Dictionary<string, string> { ["backgroundColor"] = "blue" }, 0.25);

        var ex = Assert.Throws<InvalidOperationException>(() => builder.Build());
        Assert.Contains("non-decreasing", ex.Message);
    }

    [Fact]
    public void WithTransition_Action_SetsDurationAndKeepsDefaultEasing()
    {
        var animation = new MotionBuilder()
            .To(opacity: 1)
            .WithTransition(t => t.Duration(TimeSpan.FromMilliseconds(600)))
            .Build();

        Assert.Equal(TimeSpan.FromMilliseconds(600), animation.Transition.Duration);
        Assert.Equal(MotionEasing.EaseInOut, animation.Transition.Easing);
    }

    [Fact]
    public void WithTransition_Record_IsStored()
    {
        var transition = MotionTransition.Default with
        {
            Duration = TimeSpan.FromMilliseconds(120),
            Easing = MotionEasing.Linear,
        };

        var animation = new MotionBuilder()
            .To(opacity: 1)
            .WithTransition(transition)
            .Build();

        Assert.Equal(transition, animation.Transition);
    }

    [Fact]
    public void Spring_SetsCssLinearEasingAndSuggestedDuration()
    {
        var animation = new MotionBuilder()
            .To(opacity: 1)
            .Spring()
            .Build();

        Assert.StartsWith("linear(", animation.Transition.Easing.CssValue);
        Assert.Equal(MotionEasing.Spring().SuggestedDuration, animation.Transition.Duration);
    }

    [Fact]
    public void FillForwards_DefaultsToTrue()
    {
        var animation = new MotionBuilder().To(opacity: 1).Build();

        Assert.True(animation.FillForwards);
    }

    [Fact]
    public void FillForwards_False_IsPropagated()
    {
        var animation = new MotionBuilder()
            .To(opacity: 1)
            .FillForwards(false)
            .Build();

        Assert.False(animation.FillForwards);
    }

    [Fact]
    public void Build_Twice_ReturnsEquivalentAnimations()
    {
        var builder = new MotionBuilder()
            .From(opacity: 0, y: 20)
            .To(opacity: 1, y: 0)
            .WithTransition(t => t.Duration(TimeSpan.FromMilliseconds(400)));

        var first = builder.Build();
        var second = builder.Build();

        AssertEquivalent(first, second);
    }

    [Fact]
    public void Build_ThenMoreTo_DoesNotChangeFirstResult()
    {
        var builder = new MotionBuilder().From(opacity: 0).To(opacity: 1);

        var first = builder.Build();
        builder.To(opacity: 0.5);

        Assert.Equal(2, first.Keyframes.Count);
        Assert.Equal("1", first.Keyframes[1].Properties["opacity"]);
        Assert.Equal(3, builder.Build().Keyframes.Count);
    }

    [Fact]
    public void FadeIn_BuildsZeroToOne()
    {
        var animation = MotionBuilder.FadeIn().Build();

        Assert.Equal(2, animation.Keyframes.Count);
        Assert.Equal("0", animation.Keyframes[0].Properties["opacity"]);
        Assert.Equal("1", animation.Keyframes[1].Properties["opacity"]);
    }

    [Fact]
    public void FadeOut_BuildsOneToZero()
    {
        var animation = MotionBuilder.FadeOut().Build();

        Assert.Equal(2, animation.Keyframes.Count);
        Assert.Equal("1", animation.Keyframes[0].Properties["opacity"]);
        Assert.Equal("0", animation.Keyframes[1].Properties["opacity"]);
    }

    [Fact]
    public void SlideUp_BuildsFadeInFromBelow()
    {
        var animation = MotionBuilder.SlideUp().Build();

        Assert.Equal(2, animation.Keyframes.Count);
        Assert.Equal("0", animation.Keyframes[0].Properties["opacity"]);
        Assert.Equal("translateY(20px)", animation.Keyframes[0].Properties["transform"]);
        Assert.Equal("1", animation.Keyframes[1].Properties["opacity"]);
        Assert.Equal("translateY(0px)", animation.Keyframes[1].Properties["transform"]);
    }

    [Fact]
    public void Format_UsesInvariantCulture_UnderGermanCulture()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            // Guard: make sure the culture really formats with a comma, so the test proves something.
            Assert.Equal("1,5", 1.5.ToString(CultureInfo.CurrentCulture));

            var animation = new MotionBuilder()
                .To(x: 1.5, y: -2.25, scale: 0.5, opacity: 0.75)
                .Build();

            var properties = animation.Keyframes[0].Properties;
            Assert.Equal("translateX(1.5px) translateY(-2.25px) scale(0.5)", properties["transform"]);
            Assert.Equal("0.75", properties["opacity"]);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    private static void AssertEquivalent(MotionAnimation expected, MotionAnimation actual)
    {
        // MotionAnimation's collections use reference equality, so compare their contents explicitly.
        Assert.Equal(expected.Transition, actual.Transition);
        Assert.Equal(expected.FillForwards, actual.FillForwards);
        Assert.Equal(expected.Keyframes.Count, actual.Keyframes.Count);

        for (var i = 0; i < expected.Keyframes.Count; i++)
        {
            Assert.Equal(expected.Keyframes[i].Offset, actual.Keyframes[i].Offset);
            Assert.Equal(expected.Keyframes[i].Properties, actual.Keyframes[i].Properties);
        }
    }
}
