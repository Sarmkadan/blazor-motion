# Contributing to BlazorMotion

Thanks for your interest. Short version:

## Workflow

1. Open an issue first for anything non-trivial (new API surface, breaking changes).
2. Fork, branch from `main` (`feat/...`, `fix/...`, `docs/...`).
3. Make the change. Keep PRs focused - one concern per PR.
4. `dotnet build` and `dotnet test` must pass with zero warnings (`TreatWarningsAsErrors` is on).
5. Open a PR. Describe *why*, not just *what*.

## Code style

- `.editorconfig` is the source of truth. `dotnet format` before committing.
- Public API needs XML docs. The build fails without them.
- Nullable is enabled everywhere. No `!` without a comment explaining why.
- Prefer immutable records for animation definitions; keep JS interop behind `IMotionInterop`.

## Commits

Conventional Commits: `feat:`, `fix:`, `docs:`, `refactor:`, `test:`, `chore:`.

## Tests

- Unit tests live in `tests/blazor-motion.Tests` (xUnit).
- Anything touching JS interop gets a test with a fake `IJSRuntime`.
- Bug fix -> regression test in the same PR.

## Reporting bugs

Include: Blazor hosting model (Server / WebAssembly / Auto), .NET version, browser, minimal repro.
