# Spec 026: the view model ctor must not use an original model as the updated one

**Status:** Implemented (workaround for [dotnet/roslyn#86338](https://github.com/dotnet/roslyn/issues/86338))
**Issue:** [#3295](https://github.com/unoplatform/uno.extensions/issues/3295): hot reload of an MVUX command's parameters crashes the app with an access violation
**Files touched:**

- `src/Uno.Extensions.Reactive.Generator/Bindables/ViewModelGenTool_3.cs` (and `_2`)
- `src/Uno.HotTesting.Reactive.Tests/Given_FeedsGenerator_Commands.cs`
- `src/Uno.Extensions.Reactive.UI.Tests/Given_HotReload.cs` + `MvuxHotReloadAddFieldModel.cs`

## Cause

The generated model partial is `[CreateNewOnMetadataUpdate]`: each delta emits a new `Model#N` type and leaves `Model` as is.
The generated view model has a public ctor, `: this(new Model(...))`, and a protected `ViewModel(Model model)` ctor that
initializes the members from the model.

A delta that changes the members of the view model (a command's parameters, an added feed, ...) changes the body of the
protected ctor. Roslyn emits it as an **update of the same method**, with its signature retyped to `ViewModel(Model#N)`.
The public ctor is usually not changed, so its IL still creates an original `Model` and passes it to the updated ctor.
The updated ctor then runs `Model#N` code on an instance of `Model`: if `Model#N` has fields `Model` does not have, it reads
and writes past the end of the object and corrupts the heap. The app later dies with an access violation, at a frame that
depends on what was overwritten (the two frames reported in the issue).

Reproduced without MVUX in about 30 lines with `dotnet watch` and filed as dotnet/roslyn#86338.

## Fix (workaround)

The protected ctor checks its model before using it as its own model type:

```csharp
if (!typeof(global::App.Model).IsInstanceOfType(model))
{
    return;
}
```

- In the IL of an updated ctor, `typeof(Model)` is `Model#N`, so an original instance fails the check. The ctor then only shares
  the source context, registers the model for disposal and stores it in `__reactiveModel`.
- The public ctor then self-patches (`HotReloadService.TryHotPatch`): it creates an instance of `Model#N` and initializes every
  member from it through `__Reactive_UpdateModel`, which uses the model as `dynamic` and never as a static type.
- A model of a derived type passes the check, so derived models and their view models are unaffected outside of hot reload.
- `IsInstanceOfType` is used rather than `is`: the compiler reduces `model is Model` to a null check, since `model` is already
  typed as `Model`.

Cost outside of hot reload: one type check per view model created.

Limit: if MVUX hot reload is disabled (`HotReloadSupport.State` not set) while the app still applies deltas, no self-patch runs
and the members of a view model created after such a delta stay `null`. Before the workaround, that view model ran updated code on
an original model.

Remove the check once dotnet/roslyn#86338 is fixed.

## Verification

- Generator (`Uno.HotTesting.Reactive.Tests`): `When_ModelIsNotOfTheCtorModelType_Then_CtorInitializesNothingFromIt` checks that
  the ctors of a model and a derived model check the type before initializing members. Red without the check, green with it.
- Runtime (`Uno.Extensions.Reactive.UI.Tests`, real HR delta): `When_DeltaAddsModelField_Then_NewViewModelDoesNotRunUpdatedCodeOnOriginalModel`
  adds fields to the model, written by the getter of a new state, then creates a view model and forces a GC. Run locally on Windows (desktop head,
  retries off): without the check the test app dies (`Internal CLR error. (0x80131506)`, exit code `0xC0000005`); with it, green.
- All 17 `Reactive.WinUI.Tests.Given_HotReload` tests green; `Uno.Extensions.Reactive.Tests` and `Uno.HotTesting.Reactive.Tests` green.
