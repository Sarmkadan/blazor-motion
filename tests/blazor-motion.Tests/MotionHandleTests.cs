using BlazorMotion.Interop;
using BlazorMotion.Tests.Fakes;
using Microsoft.JSInterop;

namespace BlazorMotion.Tests;

public class MotionHandleTests
{
    [Fact]
    public void Constructor_RejectsNullModule()
    {
        Assert.Throws<ArgumentNullException>("module", () => new MotionHandle(null!, [1]));
    }

    [Fact]
    public void Constructor_RejectsNullIds()
    {
        Assert.Throws<ArgumentNullException>("animationIds", () => new MotionHandle(new FakeJSObjectReference(), null!));
    }

    [Fact]
    public void Constructor_RejectsEmptyIds()
    {
        Assert.Throws<ArgumentException>("animationIds", () => new MotionHandle(new FakeJSObjectReference(), []));
    }

    [Fact]
    public async Task Finished_StaysPending_WhileJsPromiseIsPending()
    {
        var module = new FakeJSObjectReference();
        var gate = module.GateFinished(1);
        var handle = new MotionHandle(module, [1]);

        await gate.Reached;

        Assert.False(handle.Finished.IsCompleted);

        gate.Resolve(true);
        await handle.Finished;
    }

    [Fact]
    public async Task Finished_CompletesSuccessfully_WhenEveryIdResolvesTrue()
    {
        var module = new FakeJSObjectReference();
        var first = module.GateFinished(1);
        var second = module.GateFinished(2);
        var handle = new MotionHandle(module, [1, 2]);

        first.Resolve(true);
        second.Resolve(true);
        await handle.Finished;

        Assert.True(handle.Finished.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task Finished_FaultsWithOperationCanceled_WhenAnyIdResolvesFalse()
    {
        var module = new FakeJSObjectReference();
        var first = module.GateFinished(1);
        var second = module.GateFinished(2);
        var handle = new MotionHandle(module, [1, 2]);

        first.Resolve(true);
        second.Resolve(false);

        await Assert.ThrowsAsync<OperationCanceledException>(() => handle.Finished);
    }

    [Fact]
    public async Task Constructor_WithTwoIds_CallsFinishedOncePerId()
    {
        var module = new FakeJSObjectReference();
        var handle = new MotionHandle(module, [1, 2]);

        await handle.Finished;

        Assert.Equal([1, 2], IdsOf(module, "finished"));
    }

    [Fact]
    public async Task PauseAsync_CallsPauseOncePerIdInOrder()
    {
        var module = new FakeJSObjectReference();
        var handle = new MotionHandle(module, [1, 2]);

        await handle.PauseAsync();

        Assert.Equal([1, 2], IdsOf(module, "pause"));
    }

    [Fact]
    public async Task PlayAsync_CallsPlayOncePerIdInOrder()
    {
        var module = new FakeJSObjectReference();
        var handle = new MotionHandle(module, [1, 2]);

        await handle.PlayAsync();

        Assert.Equal([1, 2], IdsOf(module, "play"));
    }

    [Fact]
    public async Task CancelAsync_CallsCancelOncePerIdInOrder()
    {
        var module = new FakeJSObjectReference();
        var handle = new MotionHandle(module, [1, 2]);

        await handle.CancelAsync();

        Assert.Equal([1, 2], IdsOf(module, "cancel"));
    }

    [Fact]
    public async Task PauseAsync_AfterCancel_ThrowsInvalidOperation()
    {
        var module = new FakeJSObjectReference();
        var handle = new MotionHandle(module, [1]);
        await handle.CancelAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => handle.PauseAsync().AsTask());

        Assert.Equal("Animation was cancelled", ex.Message);
    }

    [Fact]
    public async Task DisposeAsync_WhileRunning_CallsCancel()
    {
        var module = new FakeJSObjectReference();
        var gate = module.GateFinished(1);
        var handle = new MotionHandle(module, [1]);
        await gate.Reached;

        await handle.DisposeAsync();

        Assert.Equal([1], IdsOf(module, "cancel"));
    }

    [Fact]
    public async Task DisposeAsync_AfterFinished_DoesNotCallCancel()
    {
        var module = new FakeJSObjectReference();
        var handle = new MotionHandle(module, [1]);
        await handle.Finished;

        await handle.DisposeAsync();

        Assert.Empty(IdsOf(module, "cancel"));
    }

    [Fact]
    public async Task DisposeAsync_CalledTwice_CancelsAtMostOnce()
    {
        var module = new FakeJSObjectReference();
        var gate = module.GateFinished(1);
        var handle = new MotionHandle(module, [1]);
        await gate.Reached;

        await handle.DisposeAsync();
        await handle.DisposeAsync();

        Assert.Single(IdsOf(module, "cancel"));
    }

    [Fact]
    public async Task DisposeAsync_SwallowsJSDisconnectedException()
    {
        var module = new FakeJSObjectReference();
        var gate = module.GateFinished(1);
        module.Results["cancel"] = _ => throw new JSDisconnectedException("The circuit has disconnected.");
        var handle = new MotionHandle(module, [1]);
        await gate.Reached;

        var ex = await Record.ExceptionAsync(() => handle.DisposeAsync().AsTask());

        Assert.Null(ex);
        Assert.Single(IdsOf(module, "cancel"));
    }

    /// <summary>The first argument of every call to <paramref name="identifier"/>, in call order.</summary>
    private static List<int> IdsOf(FakeJSObjectReference module, string identifier) =>
        module.Calls.Where(c => c.Identifier == identifier).Select(c => (int)c.Args![0]!).ToList();
}
