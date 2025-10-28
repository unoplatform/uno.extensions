---
uid: Uno.Extensions.Reactive.Upgrading
---

# MVUX Upgrade Guide

Concise checklist to move projects to Uno.Extensions.Reactive V5 while understanding the generator changes.

## TL;DR
- V5 keeps V4 behavior by default; no breaking change when updating packages.
- Opt-in to the new generator (view-model output) using `[assembly: BindableGenerationTool(3)]`.
- Update any code-behind or markup that instantiates generated types (rename `Bindable*Model` ➝ `*ViewModel`).

## What Changed in V5
- Generated output now defaults to `MyModelViewModel` instead of `BindableMyModel` when the new tool is enabled.
- Same analyzer still supports legacy bindable proxies for backward compatibility.
- No runtime API differences; only source-generated types differ.

## Upgrade Steps
- **Update packages**: bump Uno.Extensions.Reactive to V5.
- **Decide generator mode**:
  - Keep legacy output → no action required.
  - Adopt view-model output → add `Uno.Extensions.Reactive.Config.BindableGenerationTool(3)` at the assembly level (often in `GlobalUsings.cs`).
- **Rename references**: replace `Bindable<MyModel>` usages with `<MyModel>ViewModel` in page constructors, DI registrations, or markup.

## Sample Update
```csharp
// GlobalUsings.cs
[assembly: Uno.Extensions.Reactive.Config.BindableGenerationTool(3)]

// Before
DataContext = new BindableMainModel();

// After
DataContext = new MainViewModel();
```

## Verification
- Rebuild the project to regenerate source.
- Inspect generated files under `obj/Debug/net.../g.cs` to confirm new class names.
- Run UI smoke tests to ensure bindings resolve as expected.

## See Also
- [MVUX Overview](xref:Uno.Extensions.Mvux.Overview)
- Generator configuration reference (xref:Uno.Extensions.Reactive.Config.BindableGenerationTool)
