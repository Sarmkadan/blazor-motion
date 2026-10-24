# BlazorMotion

[![CI](https://github.com/Sarmkadan/blazor-motion/actions/workflows/ci.yml/badge.svg)](https://github.com/Sarmkadan/blazor-motion/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/BlazorMotion.svg)](https://www.nuget.org/packages/BlazorMotion)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

Framer Motion-like animation library for Blazor. Describe animations with a fluent C# API, let a tiny JS interop layer drive the browser's Web Animations API. No GSAP, no jQuery, no CSS class juggling.

> Core model (`MotionBuilder`, `MotionAnimation`, `MotionTransition`, `MotionEasing`, `MotionTimeline`) is implemented. JS interop layer is in progress.

## Features

- Fluent, strongly typed C# builder: `Motion.For(el).To(x: 100, opacity: 1).Spring()`
- Declarative `<Motion>` component with `Initial` / `Animate` / `Exit` states (planned)
- Timelines: sequence, stagger and overlap multiple animations
- Easing presets plus cubic-bezier and spring physics
- Backed by the Web Animations API - runs off the main thread where the browser allows
- Works in Blazor Server, WebAssembly and Auto render modes
- Zero runtime JS dependencies; the JS module is a few KB

## Installation

```bash
dotnet add package BlazorMotion
```

Register in `Program.cs`:

```csharp
builder.Services.AddBlazorMotion();
```

## Quick start

```razor
@inject IMotionInterop Motion

<div @ref="_box" class="box">Hello</div>
<button @onclick="Animate">Go</button>

@code {
    private ElementReference _box;

    private async Task Animate()
    {
        var animation = new MotionBuilder()
            .To(x: 200, opacity: 1, scale: 1.1)
            .WithTransition(t => t.Duration(TimeSpan.FromMilliseconds(600)).Ease(MotionEasing.EaseOut))
            .Build();

        await Motion.AnimateAsync(_box, animation);
    }
}
```

## Setup and hosting models

### Registration

```csharp
builder.Services.AddBlazorMotion();
```

To change the global options, use the overload that takes a delegate:

```csharp
builder.Services.AddBlazorMotion(o =>
{
    o.DefaultTransition = MotionTransition.Default with
    {
        Duration = TimeSpan.FromMilliseconds(400),
        Easing = MotionEasing.EaseOut,
    };
    o.RespectReducedMotion = true;
});
```

| Option | Default | Notes |
| --- | --- | --- |
| `DefaultTransition` | 300 ms, no delay, `EaseInOut`, plays once | Validated (`Duration` and `Delay` must not be negative), but not applied yet: animations still use the transition they were built with, which starts from `MotionTransition.Default`. |
| `ModulePath` | `./_content/BlazorMotion/blazor-motion.js` | Must not be empty. Relative to the page's base URI. Change it only if you serve the module yourself. |
| `RespectReducedMotion` | `true` | Forwarded to the JS module on first import. The module does not collapse durations yet, so this has no visible effect today. |

Options are validated the first time they are resolved, which is the first interop call. An invalid value throws `OptionsValidationException` at that point, not at startup.

### The JS module

The module ships as a static web asset at `_content/BlazorMotion/blazor-motion.js`. You do not add a `<script>` tag. The first interop call imports it with a dynamic `import()`, passes it the global settings once, and reuses it for every later call from the same `IMotionInterop` instance.

### Hosting models

`IMotionInterop` is registered as scoped and uses only asynchronous JS interop, so the same code runs under every render mode.

- **Blazor Server.** Each circuit gets its own interop instance and its own module. If the circuit disconnects, interop calls throw `JSDisconnectedException`. `DisposeAsync` on a handle, and on the interop itself, swallows that exception, because the browser-side animations went away with the circuit. Other calls do not swallow it: catch it if you start animations over an unreliable connection.
- **Blazor WebAssembly.** The code runs in the browser with no circuit, so the scope spans the whole app and the interop behaves like a singleton. `JSDisconnectedException` does not occur.
- **Auto.** A component renders on Server first and moves to WebAssembly once the runtime has downloaded. The component is re-created on that switch, so do not keep an `IMotionHandle` across it. Start the animation again in `OnAfterRenderAsync` on the new side.

In every mode, an `ElementReference` from `@ref` is only populated after the element has rendered. Start animations in `OnAfterRenderAsync` or in an event handler, never in the constructor or `OnInitialized`. A default `ElementReference` throws `ArgumentException` ("Element is not rendered yet").

```csharp
@inject IMotionInterop Motion

<div @ref="_box" class="box">Hello</div>

@code {
    private ElementReference _box;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var enter = new MotionBuilder()
                .From(opacity: 0, y: 20)
                .To(opacity: 1, y: 0)
                .Build();

            await Motion.AnimateAsync(_box, enter);
        }
    }
}
```

## Animations in depth

### Multi-step keyframes

Call `.To(...)` several times to create multi-step animations. Each call appends a keyframe:

```csharp
var bounce = new MotionBuilder()
    .From(y: -40, opacity: 0)
    .To(y: 0, opacity: 1)
    .To(y: -15)
    .To(y: 0)
    .WithTransition(t => t.Duration(TimeSpan.FromMilliseconds(800)))
    .Build();
```

### Arbitrary CSS properties

For properties that `.To(x, y, scale, rotate, opacity)` does not cover, pass a dictionary:

```csharp
var colorShift = new MotionBuilder()
    .To(new Dictionary<string, string>
    {
        ["backgroundColor"] = "#ff5722"
    }, offset: 0.5)
    .To(new Dictionary<string, string>
    {
        ["backgroundColor"] = "#4caf50"
    })
    .Build();
```

The `offset` parameter (0..1) pins a keyframe to a specific point; `null` distributes evenly.

### Transitions

Configure timing via the `MotionTransitionBuilder`:

```csharp
var anim = new MotionBuilder()
    .From(opacity: 0)
    .To(opacity: 1)
    .WithTransition(t => t
        .Duration(TimeSpan.FromMilliseconds(500))
        .Delay(TimeSpan.FromMilliseconds(200))
        .Ease(MotionEasing.CubicBezier(0.4, 0, 0.2, 1))
        .Repeat(2, MotionDirection.Alternate))
    .Build();
```

Loop forever with `.Forever()`:

```csharp
.WithTransition(t => t
    .Duration(TimeSpan.FromMilliseconds(1000))
    .Forever())
```

### Springs

Use `.Spring()` on the builder for spring-eased transitions. It emits a CSS `linear()` easing
approximation so it works in the Web Animations API without JS ticking:

```csharp
var springy = new MotionBuilder()
    .From(scale: 0.8)
    .To(scale: 1.0)
    .Spring(stiffness: 200, damping: 12)
    .Build();
```

Or apply spring easing directly: `MotionEasing.Spring(stiffness: 200, damping: 12, mass: 1)`.

### Reversed and reused animations

Derive exit animations from enter animations with `Reversed()`. Keyframes are reversed and
explicit offsets are mapped to `1 - offset`:

```csharp
var enter = new MotionBuilder()
    .From(opacity: 0, y: 20)
    .To(opacity: 1, y: 0)
    .Build();

var exit = enter.Reversed();
```

Reuse an animation across elements by swapping just the transition:

```csharp
var fast = enter.WithTransition(enter.Transition with
{
    Duration = TimeSpan.FromMilliseconds(150)
});
```

Adjust delay or duration individually:

```csharp
var delayed = enter.WithDelay(TimeSpan.FromMilliseconds(300));
var slow = enter.WithDuration(TimeSpan.FromMilliseconds(1000));
```

Create an animation from raw keyframes:

```csharp
var custom = MotionAnimation.FromKeyframes(new[]
{
    new MotionKeyframe(new Dictionary<string, string> { ["opacity"] = "0" }),
    new MotionKeyframe(new Dictionary<string, string> { ["opacity"] = "1" }),
});
```

## Timelines

`MotionTimeline` orchestrates several animations across several elements.

- `Add(target, animation, offset?)` - appends sequentially. By default starts when the previous entry ends. A negative offset overlaps the previous entry.
- `AddAt(target, animation, at)` - starts at an absolute position in the timeline.
- `Stagger(targets, animation, stagger)` - applies the same animation to many elements, each starting `stagger` after the previous one.
- `TotalDuration` - total duration including the longest-running entry and its delay/repeat.

```csharp
var fadeIn = new MotionBuilder()
    .From(opacity: 0, y: 20)
    .To(opacity: 1, y: 0)
    .WithTransition(t => t.Duration(TimeSpan.FromMilliseconds(400)))
    .Build();

var timeline = new MotionTimeline()
    .Add(_title, fadeIn)
    .Add(_subtitle, fadeIn, offset: TimeSpan.FromMilliseconds(150))
    .Stagger(_cards, fadeIn, TimeSpan.FromMilliseconds(80));

await Motion.PlayAsync(timeline);
```

## Controlling running animations

`AnimateAsync` and `PlayAsync` (for timelines) return an `IMotionHandle`. Await `Finished` to wait for the animation to end:

```csharp
await using var handle = await Motion.AnimateAsync(_box, anim);
await handle.Finished;
```

The handle also controls playback:

- `await handle.PauseAsync();` and `await handle.PlayAsync();` pause and resume the animation.
- `await handle.CancelAsync();` stops the animation and removes its effects, including the end state kept by `FillForwards`.
- `Finished` completes when the animation ends naturally. If it is cancelled, `Finished` faults with `OperationCanceledException`, so catch that if you cancel while someone is waiting on it:

```csharp
try
{
    await handle.Finished;
}
catch (OperationCanceledException)
{
    // Cancelled before it finished.
}
```

- Calling `PauseAsync` or `PlayAsync` after `CancelAsync` throws `InvalidOperationException`.
- `DisposeAsync` (the `await using` above) cancels the animation if it is still running, and does nothing once it has finished. If you want the end state to remain, await `Finished` before the handle goes out of scope.

To stop everything on an element, including animations that were not started through BlazorMotion, use `CancelAllAsync`:

```csharp
await Motion.CancelAllAsync(_box);
```

Handles from `PlayAsync` cover the whole timeline. `Finished` completes when the last entry ends and faults if any entry is cancelled.

## API overview

| Type | Role |
| --- | --- |
| `MotionBuilder` | Fluent builder. Produces an immutable `MotionAnimation`. |
| `MotionAnimation` | Pure data: target keyframes + `MotionTransition`. Serializable, reusable. |
| `MotionKeyframe` | A single keyframe: CSS property values at one point, with optional offset. |
| `MotionTransition` | Duration, delay, easing, repeat, direction. |
| `MotionTransitionBuilder` | Fluent builder for `MotionTransition` (used inside `WithTransition`). |
| `MotionEasing` | Named presets (`Linear`, `EaseInOut`, ...), `CubicBezier(...)` and `Spring(...)`. |
| `MotionDirection` | Playback direction enum: `Normal`, `Reverse`, `Alternate`. |
| `MotionTimeline` | Orchestrates multiple animations with offsets and staggers. |
| `MotionTimelineEntry` | One entry in a timeline: target element, animation, start offset. |
| `IMotionInterop` | The JS bridge. Animate an element, play a timeline, cancel, await completion. |
| `IMotionHandle` | Handle to a running animation. Pause, play, cancel, await `Finished`. |

## Troubleshooting

- **`blazor-motion: animate needs a rendered element with the Web Animations API (animate())`.** The element is not in the DOM yet, has been removed, or the reference does not point at an element. The JS guard throws this instead of a bare `element.animate is not a function`. Start the animation after `OnAfterRenderAsync` (see [Hosting models](#hosting-models)), and make sure the reference comes from `@ref` on an HTML element.
- **The animation snaps back at the end.** `FillForwards` is turned off, so the element returns to its CSS state when the animation finishes. Keep the default (`FillForwards` is `true`), or set the final state in CSS. Disposing or cancelling the handle early also removes the end state.
- **Springs look wrong in older browsers.** `Spring()` and `MotionEasing.Spring()` emit a `linear(...)` easing, which requires `linear()` easing support in the browser (Chrome 113+, Firefox 112+, Safari 17.2+). Without it the curve is rejected and the motion will not look like a spring. Use a cubic-bezier easing such as `MotionEasing.BackOut` if you must support older browsers.
- **Reduced motion is not respected yet.** `RespectReducedMotion` is forwarded to the JS module, but durations are not collapsed for `prefers-reduced-motion: reduce`. Check the media query in your own code until this is implemented.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md). Issues and PRs welcome.

## License

[MIT](LICENSE) - Copyright 2026 Vladyslav Zaiets
