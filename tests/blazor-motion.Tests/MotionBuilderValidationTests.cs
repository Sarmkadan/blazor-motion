namespace BlazorMotion.Tests;

public class MotionBuilderValidationTests
{
    [Theory]
    [InlineData(double.NaN)]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void From_RejectsOpacityOutsideUnitRange(double opacity)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new MotionBuilder().From(opacity: opacity));
        Assert.Equal("opacity", ex.ParamName);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void To_RejectsOpacityOutsideUnitRange(double opacity)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new MotionBuilder().To(opacity: opacity));
        Assert.Equal("opacity", ex.ParamName);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(-1)]
    public void To_RejectsNegativeOrNaNScale(double scale)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new MotionBuilder().To(scale: scale));
        Assert.Equal("scale", ex.ParamName);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void From_RejectsNonFiniteX(double x)
    {
        var ex = Assert.Throws<ArgumentException>(() => new MotionBuilder().From(x: x));
        Assert.Equal("x", ex.ParamName);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void To_RejectsNonFiniteY(double y)
    {
        var ex = Assert.Throws<ArgumentException>(() => new MotionBuilder().To(y: y));
        Assert.Equal("y", ex.ParamName);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void To_RejectsNonFiniteRotate(double rotate)
    {
        var ex = Assert.Throws<ArgumentException>(() => new MotionBuilder().To(rotate: rotate));
        Assert.Equal("rotate", ex.ParamName);
    }

    [Fact]
    public void To_Dictionary_RejectsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new MotionBuilder().To((IReadOnlyDictionary<string, string>)null!));
    }

    [Fact]
    public void To_Dictionary_RejectsEmpty()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new MotionBuilder().To(new Dictionary<string, string>()));
        Assert.Equal("cssProperties", ex.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void To_Dictionary_RejectsBlankKey(string key)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new MotionBuilder().To(new Dictionary<string, string> { [key] = "1" }));
        Assert.Equal("cssProperties", ex.ParamName);
    }

    [Fact]
    public void To_Dictionary_RejectsNullKey()
    {
        // Dictionary<,> cannot hold a null key, so use a read-only view that can.
        var cssProperties = new PairsDictionary(new KeyValuePair<string, string>(null!, "1"));

        var ex = Assert.Throws<ArgumentException>(() => new MotionBuilder().To(cssProperties));
        Assert.Equal("cssProperties", ex.ParamName);
    }

    [Fact]
    public void To_Dictionary_RejectsNullValue()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new MotionBuilder().To(new Dictionary<string, string> { ["color"] = null! }));
        Assert.Equal("cssProperties", ex.ParamName);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void To_Dictionary_RejectsOffsetOutsideUnitRange(double offset)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MotionBuilder().To(new Dictionary<string, string> { ["color"] = "red" }, offset));
        Assert.Equal("offset", ex.ParamName);
    }

    [Fact]
    public void Spring_RejectsNonPositiveStiffness()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new MotionBuilder().Spring(stiffness: 0));
        Assert.Equal("stiffness", ex.ParamName);

        Assert.Throws<ArgumentOutOfRangeException>(() => new MotionBuilder().Spring(stiffness: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new MotionBuilder().Spring(stiffness: double.NaN));
    }

    [Fact]
    public void Spring_RejectsNegativeDamping()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new MotionBuilder().Spring(damping: -0.5));
        Assert.Equal("damping", ex.ParamName);
    }

    [Fact]
    public void WithTransition_Action_RejectsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new MotionBuilder().WithTransition((Action<MotionTransitionBuilder>)null!));
    }

    [Fact]
    public void WithTransition_Transition_RejectsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new MotionBuilder().WithTransition((MotionTransition)null!));
    }

    [Fact]
    public void ValidBoundaryValues_AreAccepted()
    {
        var builder = new MotionBuilder()
            .From(x: -10, y: 0, scale: 0, rotate: 360, opacity: 0)
            .To(x: 10, y: -5, scale: 2.5, rotate: -45, opacity: 1)
            .To(new Dictionary<string, string> { ["filter"] = "blur(0px)" }, offset: 0)
            .To(new Dictionary<string, string> { ["filter"] = "blur(4px)" }, offset: 1)
            .Spring(stiffness: 1, damping: 0);

        var animation = builder.Build();

        Assert.Equal(4, animation.Keyframes.Count);
    }

    private sealed class PairsDictionary(params KeyValuePair<string, string>[] pairs) : IReadOnlyDictionary<string, string>
    {
        public int Count => pairs.Length;

        public IEnumerable<string> Keys => pairs.Select(p => p.Key);

        public IEnumerable<string> Values => pairs.Select(p => p.Value);

        public string this[string key] => throw new NotSupportedException();

        public bool ContainsKey(string key) => throw new NotSupportedException();

        public bool TryGetValue(string key, out string value) => throw new NotSupportedException();

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => pairs.AsEnumerable().GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
