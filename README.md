# BlazorMotion

[![CI](https://github.com/Sarmkadan/blazor-motion/actions/workflows/ci.yml/badge.svg)](https://github.com/Sarmkadan/blazor-motion/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/BlazorMotion.svg)](https://www.nuget.org/packages/BlazorMotion)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

Framer Motion-like animation library for Blazor. Describe animations with a fluent C# API, let a tiny JS interop layer drive the browser's Web Animations API. No GSAP, no jQuery, no CSS class juggling.

> Status: early scaffold. Public API shape is defined, implementation in progress.

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

Sequencing several elements:

```csharp
var timeline = new MotionTimeline()
    .Add(_title, fadeIn)
    .Add(_subtitle, fadeIn, offset: TimeSpan.FromMilliseconds(150))
    .Stagger(_cards, slideUp, TimeSpan.FromMilliseconds(80));

await Motion.PlayAsync(timeline);
```

## API overview

| Type | Role |
| --- | --- |
| `MotionBuilder` | Fluent builder. Produces an immutable `MotionAnimation`. |
| `MotionAnimation` | Pure data: target keyframes + `MotionTransition`. Serializable, reusable. |
| `MotionTransition` | Duration, delay, easing, repeat, direction. |
| `MotionEasing` | Named presets (`Linear`, `EaseInOut`, ...) and `CubicBezier(...)`. |
| `MotionTimeline` | Orchestrates multiple animations with offsets and staggers. |
| `IMotionInterop` | The JS bridge. Animate an element, play a timeline, cancel, await completion. |

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md). Issues and PRs welcome.

## License

[MIT](LICENSE) - Copyright 2026 Vladyslav Zaiets
