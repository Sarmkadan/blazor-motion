namespace BlazorMotion.Tests;

public class MotionTransitionBuilderTests
{
    [Fact]
    public void Default_HasExpectedValues()
    {
        var transition = MotionTransition.Default;

        Assert.Equal(TimeSpan.FromMilliseconds(300), transition.Duration);
        Assert.Equal(TimeSpan.Zero, transition.Delay);
        Assert.Equal(MotionEasing.EaseInOut, transition.Easing);
        Assert.Equal(0, transition.Repeat);
        Assert.Equal(MotionDirection.Normal, transition.Direction);
    }

    [Fact]
    public void Build_WithoutCalls_EqualsDefault()
    {
        var built = new MotionTransitionBuilder().Build();

        Assert.Equal(MotionTransition.Default, built);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(1500)]
    public void Duration_SetsOnlyDuration(int milliseconds)
    {
        var duration = TimeSpan.FromMilliseconds(milliseconds);

        var built = new MotionTransitionBuilder().Duration(duration).Build();

        Assert.Equal(MotionTransition.Default with { Duration = duration }, built);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(2000)]
    public void Delay_SetsOnlyDelay(int milliseconds)
    {
        var delay = TimeSpan.FromMilliseconds(milliseconds);

        var built = new MotionTransitionBuilder().Delay(delay).Build();

        Assert.Equal(MotionTransition.Default with { Delay = delay }, built);
    }

    [Fact]
    public void Ease_SetsOnlyEasing()
    {
        var easing = MotionEasing.EaseInOutCubic;

        var built = new MotionTransitionBuilder().Ease(easing).Build();

        Assert.Equal(MotionTransition.Default with { Easing = easing }, built);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(10)]
    public void Repeat_SetsOnlyCountAndKeepsNormalDirection(int count)
    {
        var built = new MotionTransitionBuilder().Repeat(count).Build();

        Assert.Equal(MotionTransition.Default with { Repeat = count }, built);
    }

    [Theory]
    [InlineData(MotionDirection.Normal)]
    [InlineData(MotionDirection.Reverse)]
    [InlineData(MotionDirection.Alternate)]
    public void Repeat_SetsCountAndDirection(MotionDirection direction)
    {
        var built = new MotionTransitionBuilder().Repeat(3, direction).Build();

        Assert.Equal(3, built.Repeat);
        Assert.Equal(direction, built.Direction);
    }

    [Fact]
    public void Repeat_WithReverse_SetsBothCountAndDirection()
    {
        var built = new MotionTransitionBuilder().Repeat(3, MotionDirection.Reverse).Build();

        Assert.Equal(MotionTransition.Default with { Repeat = 3, Direction = MotionDirection.Reverse }, built);
    }

    [Fact]
    public void Forever_DefaultsToAlternate()
    {
        var built = new MotionTransitionBuilder().Forever().Build();

        Assert.Equal(int.MaxValue, built.Repeat);
        Assert.Equal(MotionDirection.Alternate, built.Direction);
        Assert.Equal(MotionTransition.Default with { Repeat = int.MaxValue, Direction = MotionDirection.Alternate }, built);
    }

    [Fact]
    public void Forever_WithNormal_SetsNormalDirection()
    {
        var built = new MotionTransitionBuilder().Forever(MotionDirection.Normal).Build();

        Assert.Equal(int.MaxValue, built.Repeat);
        Assert.Equal(MotionDirection.Normal, built.Direction);
    }

    [Fact]
    public void SeededConstructor_StartsFromSeed()
    {
        var seed = new MotionTransition(
            Duration: TimeSpan.FromSeconds(1),
            Delay: TimeSpan.FromMilliseconds(50),
            Easing: MotionEasing.EaseInOutCubic,
            Repeat: 2,
            Direction: MotionDirection.Reverse);

        var built = new MotionTransitionBuilder(seed).Build();

        Assert.Equal(seed, built);
    }

    [Fact]
    public void SeededConstructor_AllowsOverridingSeedValues()
    {
        var seed = MotionTransition.Default with { Repeat = 2, Direction = MotionDirection.Reverse };

        var built = new MotionTransitionBuilder(seed).Duration(TimeSpan.FromSeconds(2)).Build();

        Assert.Equal(seed with { Duration = TimeSpan.FromSeconds(2) }, built);
    }

    [Fact]
    public void Build_TwiceYieldsEqualRecords()
    {
        var builder = new MotionTransitionBuilder()
            .Duration(TimeSpan.FromMilliseconds(500))
            .Repeat(2, MotionDirection.Alternate);

        var first = builder.Build();
        var second = builder.Build();

        Assert.Equal(first, second);
    }

    [Fact]
    public void MutatingBuilderAfterBuild_DoesNotChangeFirstResult()
    {
        var builder = new MotionTransitionBuilder();
        var first = builder.Build();

        builder
            .Duration(TimeSpan.FromSeconds(5))
            .Delay(TimeSpan.FromSeconds(1))
            .Ease(MotionEasing.EaseInOutCubic)
            .Forever(MotionDirection.Reverse);

        Assert.Equal(MotionTransition.Default, first);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-300)]
    public void Duration_NegativeThrows(int milliseconds)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => new MotionTransitionBuilder().Duration(TimeSpan.FromMilliseconds(milliseconds)));

        Assert.Equal("duration", ex.ParamName);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-50)]
    public void Delay_NegativeThrows(int milliseconds)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => new MotionTransitionBuilder().Delay(TimeSpan.FromMilliseconds(milliseconds)));

        Assert.Equal("delay", ex.ParamName);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void Repeat_NegativeCountThrows(int count)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new MotionTransitionBuilder().Repeat(count));

        Assert.Equal("count", ex.ParamName);
    }
}
