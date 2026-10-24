using BlazorMotion.Interop;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace BlazorMotion.Tests.Interop;

public class MotionInteropGuardTests
{
    private static readonly ElementReference _rendered = new("blazor-motion-target");

    [Fact]
    public void Constructor_RejectsNullJsRuntime()
    {
        Assert.Throws<ArgumentNullException>("js", () => new MotionInterop(null!, Options.Create(new MotionOptions())));
    }

    [Fact]
    public void Constructor_RejectsNullOptions()
    {
        Assert.Throws<ArgumentNullException>("options", () => new MotionInterop(new FakeJsRuntime(), null!));
    }

    [Fact]
    public async Task AnimateAsync_RejectsNullAnimation()
    {
        var sut = CreateSut();

        await Assert.ThrowsAsync<ArgumentNullException>(() => sut.AnimateAsync(_rendered, null!).AsTask());
    }

    [Fact]
    public async Task AnimateAsync_RejectsAnimationWithoutKeyframes()
    {
        var sut = CreateSut();
        var animation = new MotionAnimation([], MotionTransition.Default);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => sut.AnimateAsync(_rendered, animation).AsTask());
        Assert.Equal("animation", ex.ParamName);
    }

    [Fact]
    public async Task AnimateAsync_RejectsDefaultElementReference()
    {
        var sut = CreateSut();
        var animation = MotionAnimation.FromKeyframes([new MotionKeyframe(new Dictionary<string, string> { ["opacity"] = "1" })]);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => sut.AnimateAsync(default, animation).AsTask());
        Assert.Equal("Element is not rendered yet; call after OnAfterRenderAsync", ex.Message);
    }

    [Fact]
    public async Task CancelAllAsync_RejectsDefaultElementReference()
    {
        var sut = CreateSut();

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => sut.CancelAllAsync(default).AsTask());
        Assert.Equal("Element is not rendered yet; call after OnAfterRenderAsync", ex.Message);
    }

    [Fact]
    public async Task PlayAsync_RejectsNullTimeline()
    {
        var sut = CreateSut();

        await Assert.ThrowsAsync<ArgumentNullException>(() => sut.PlayAsync(null!).AsTask());
    }

    [Fact]
    public async Task PlayAsync_RejectsEmptyTimeline()
    {
        var sut = CreateSut();

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => sut.PlayAsync(new MotionTimeline()).AsTask());
        Assert.Equal("Timeline has no entries", ex.Message);
    }

    [Fact]
    public async Task EveryPublicMethod_ThrowsObjectDisposedAfterDispose()
    {
        var sut = CreateSut();
        var animation = MotionAnimation.FromKeyframes([new MotionKeyframe(new Dictionary<string, string> { ["opacity"] = "1" })]);
        var timeline = new MotionTimeline().Add(_rendered, animation);

        await sut.DisposeAsync();
        await sut.DisposeAsync(); // idempotent, must not throw

        await Assert.ThrowsAsync<ObjectDisposedException>(() => sut.AnimateAsync(_rendered, animation).AsTask());
        await Assert.ThrowsAsync<ObjectDisposedException>(() => sut.PlayAsync(timeline).AsTask());
        await Assert.ThrowsAsync<ObjectDisposedException>(() => sut.CancelAllAsync(_rendered).AsTask());
    }

    [Fact]
    public void MotionHandle_RejectsEmptyIdList()
    {
        Assert.Throws<ArgumentException>("animationIds", () => new MotionHandle(new FakeModule(), []));
    }

    [Fact]
    public async Task MotionHandle_PauseAndPlayThrowAfterCancel()
    {
        var handle = new MotionHandle(new FakeModule(), [1]);

        await handle.CancelAsync();

        var pause = await Assert.ThrowsAsync<InvalidOperationException>(() => handle.PauseAsync().AsTask());
        Assert.Equal("Animation was cancelled", pause.Message);

        var play = await Assert.ThrowsAsync<InvalidOperationException>(() => handle.PlayAsync().AsTask());
        Assert.Equal("Animation was cancelled", play.Message);
    }

    private static MotionInterop CreateSut() =>
        new(new FakeJsRuntime(), Options.Create(new MotionOptions()));

    private sealed class FakeJsRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            throw new InvalidOperationException("JS must not be reached by guard tests.");

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            throw new InvalidOperationException("JS must not be reached by guard tests.");
    }

    /// <summary>Accepts every call and returns the default result, so the handle can be driven without a browser.</summary>
    private sealed class FakeModule : IJSObjectReference
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            new(default(TValue)!);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            new(default(TValue)!);

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
