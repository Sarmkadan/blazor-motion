# BlazorMotion

Framer Motion-like animation library for Blazor. Fluent C# API describes animations; a thin JS interop layer drives the Web Animations API in the browser.

## Build & test

```bash
dotnet build                                  # whole solution, warnings are errors
dotnet test                                   # xUnit, tests/blazor-motion.Tests
dotnet test --filter "FullyQualifiedName~Timeline"   # subset
dotnet format --verify-no-changes             # style check (.editorconfig)
dotnet pack src/blazor-motion -c Release -o artifacts
```

Local SDK is .NET 10; the library targets `net9.0`, the test project `net10.0` (it references the lib, that is fine). CI installs both.

## Layout

```
src/blazor-motion/          library (PackageId BlazorMotion, namespace BlazorMotion)
  IMotionInterop.cs         JS bridge contract - the only place JS is touched
  MotionAnimation.cs        immutable animation definition (record)
  MotionBuilder.cs          fluent builder producing MotionAnimation
  MotionTimeline.cs         sequencing/orchestration of several animations
  MotionTransition.cs       easing, duration, delay, repeat
  wwwroot/                  (planned) blazor-motion.js - Web Animations API adapter
tests/blazor-motion.Tests/  xUnit
```

## Architecture

- **Definition vs execution.** `MotionBuilder` -> `MotionAnimation` (pure data, serializable). Execution happens only via `IMotionInterop`, which calls `blazor-motion.js`. Nothing in the C# model may depend on `IJSRuntime` directly.
- **One JS module.** `wwwroot/blazor-motion.js` is loaded lazily with `IJSRuntime.InvokeAsync<IJSObjectReference>("import", ...)`. Keep it dependency-free (no GSAP at runtime - "gsap" in tags refers to API inspiration).
- **Components later.** `<Motion>` razor component wraps `ElementReference` + builder. Add it only after the core model is stable.
- **Hosting models.** Must work in Server, WebAssembly and Auto. No synchronous JS interop (`IJSInProcessRuntime`) in the public path.

## Conventions

- File-scoped namespaces, `BlazorMotion.*`.
- Public API: XML docs mandatory (build fails otherwise). Stubs throw `NotImplementedException` until implemented - replace, do not leave both.
- Immutable records for definitions; `with` for variation. Builders are the only mutable objects.
- Time values are `TimeSpan`, not raw ms. Convert at the interop boundary.
- Easing is a value type (`MotionEasing`) with named presets + cubic-bezier; no strings in the public API.
- Tests: fake `IJSRuntime`, assert the payload sent to JS. No browser automation in unit tests.
- Commits: Conventional Commits. No `Co-Authored-By` lines.
