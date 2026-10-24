using BlazorMotion.Interop;
using BlazorMotion.Tests.Fakes;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;

namespace BlazorMotion.Tests;

public class MotionInteropTests
{
    private static readonly ElementReference _target = new("blazor-motion-target");

    [Fact]
    public async Task AnimateAsync_FirstCall_ImportsModuleOnceFromOptionsPath()
    {
        var js = new FakeJSRuntime();
        var sut = CreateSut(js, new MotionOptions { ModulePath = "./_content/Custom/motion.js" });

        await sut.AnimateAsync(_target, Animation());

        var import = Assert.Single(CallsTo(js.Calls), c => c.Identifier == "import");
        Assert.Equal(new object?[] { "./_content/Custom/motion.js" }, import.Args);
    }

    [Fact]
    public async Task AnimateAsync_SecondCall_DoesNotImportAgain()
    {
        var js = new FakeJSRuntime();
        var sut = CreateSut(js);

        await sut.AnimateAsync(_target, Animation());
        await sut.AnimateAsync(_target, Animation());

        Assert.Single(CallsTo(js.Calls), c => c.Identifier == "import");
        Assert.Equal(2, CallsTo(js.Module.Calls).Count(c => c.Identifier == "animate"));
    }

    [Fact]
    public async Task AnimateAsync_ConcurrentFirstCalls_ImportAndConfigureExactlyOnce()
    {
        var js = new FakeJSRuntime();
        var sut = CreateSut(js);

        await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => sut.AnimateAsync(_target, Animation()).AsTask()));

        Assert.Single(CallsTo(js.Calls), c => c.Identifier == "import");
        Assert.Single(CallsTo(js.Module.Calls), c => c.Identifier == "configure");
        Assert.Equal(5, CallsTo(js.Module.Calls).Count(c => c.Identifier == "animate"));
    }

    [Fact]
    public async Task AnimateAsync_InvokesAnimateWithElementAndPayload()
    {
        var js = new FakeJSRuntime();
        var sut = CreateSut(js);

        await sut.AnimateAsync(_target, Animation(TimeSpan.FromMilliseconds(600)));

        var animate = Assert.Single(CallsTo(js.Module.Calls), c => c.Identifier == "animate");
        Assert.Equal(_target, animate.Args![0]);

        var payload = Assert.IsType<AnimationPayload>(animate.Args[1]);
        Assert.Equal(600d, payload.Timing.Duration);
    }

    [Fact]
    public async Task AnimateAsync_ReturnsHandleWithAnimationIdFromJs()
    {
        var sut = CreateSut(new FakeJSRuntime());

        var handle = Assert.IsType<MotionHandle>(await sut.AnimateAsync(_target, Animation()));

        Assert.Equal(new[] { 7 }, handle.AnimationIds);
    }

    [Fact]
    public async Task PlayAsync_InvokesPlayTimelineOnceWithOnePayloadPerEntry()
    {
        var js = new FakeJSRuntime();
        var sut = CreateSut(js);
        var targets = new[] { new ElementReference("a"), new ElementReference("b"), new ElementReference("c") };
        var timeline = new MotionTimeline()
            .AddAt(targets[0], Animation(), TimeSpan.Zero)
            .AddAt(targets[1], Animation(), TimeSpan.FromMilliseconds(150))
            .AddAt(targets[2], Animation(), TimeSpan.FromMilliseconds(300));

        var handle = Assert.IsType<MotionHandle>(await sut.PlayAsync(timeline));

        var play = Assert.Single(CallsTo(js.Module.Calls), c => c.Identifier == "playTimeline");
        var entries = Assert.IsAssignableFrom<IReadOnlyList<TimelineEntryPayload>>(play.Args![0]);
        Assert.Equal(timeline.Entries.Count, entries.Count);
        Assert.Equal(targets, entries.Select(e => e.Target));
        Assert.Equal(new double[] { 0, 150, 300 }, entries.Select(e => e.StartAt));
        Assert.Equal(new[] { 1, 2, 3 }, handle.AnimationIds);
    }

    [Fact]
    public async Task CancelAllAsync_InvokesCancelAllWithElement()
    {
        var js = new FakeJSRuntime();
        var sut = CreateSut(js);

        await sut.CancelAllAsync(_target);

        var cancel = Assert.Single(CallsTo(js.Module.Calls), c => c.Identifier == "cancelAll");
        Assert.Equal(new object?[] { _target }, cancel.Args);
    }

    [Fact]
    public async Task AnimateAsync_NullAnimation_ThrowsArgumentNull()
    {
        var sut = CreateSut(new FakeJSRuntime());

        await Assert.ThrowsAsync<ArgumentNullException>(() => sut.AnimateAsync(_target, null!).AsTask());
    }

    [Fact]
    public async Task PlayAsync_NullTimeline_ThrowsArgumentNull()
    {
        var sut = CreateSut(new FakeJSRuntime());

        await Assert.ThrowsAsync<ArgumentNullException>(() => sut.PlayAsync(null!).AsTask());
    }

    [Fact]
    public async Task PlayAsync_EmptyTimeline_ThrowsArgumentException()
    {
        var sut = CreateSut(new FakeJSRuntime());

        await Assert.ThrowsAsync<ArgumentException>(() => sut.PlayAsync(new MotionTimeline()).AsTask());
    }

    [Fact]
    public async Task DisposeAsync_DisposesModuleAndRejectsFurtherCalls()
    {
        var js = new FakeJSRuntime();
        var sut = CreateSut(js);
        await sut.AnimateAsync(_target, Animation()); // imports the module

        await sut.DisposeAsync();

        Assert.True(js.Module.IsDisposed);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => sut.AnimateAsync(_target, Animation()).AsTask());
    }

    private static MotionInterop CreateSut(FakeJSRuntime js, MotionOptions? options = null) =>
        new(js, Options.Create(options ?? new MotionOptions()));

    private static MotionAnimation Animation(TimeSpan? duration = null) =>
        new(
            [
                new MotionKeyframe(new Dictionary<string, string> { ["opacity"] = "0" }, 0),
                new MotionKeyframe(new Dictionary<string, string> { ["opacity"] = "1" }, 1),
            ],
            MotionTransition.Default with { Duration = duration ?? MotionTransition.Default.Duration });

    /// <summary>Copies the recorded calls under the list's lock, so a background watcher cannot modify it mid-enumeration.</summary>
    private static IReadOnlyList<JSCall> CallsTo(List<JSCall> calls)
    {
        lock (calls)
        {
            return calls.ToArray();
        }
    }
}
