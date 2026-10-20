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

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md). Issues and PRs welcome.

## License

[MIT](LICENSE) - Copyright 2026 Vladyslav Zaiets
