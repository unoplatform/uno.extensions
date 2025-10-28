---
uid: Uno.Extensions.Mvux.HowToMvuxProject
---

# MVUX Project Setup

Concise instructions for starting a new Uno Platform project with MVUX or enabling MVUX in an existing solution.

## TL;DR
- Run `uno check` and complete the Uno environment setup before proceeding.
- New project: pick MVUX in the Visual Studio wizard or pass `-presentation mvux` when using `dotnet new unoapp`.
- Existing project: add `MVUX;` to the `<UnoFeatures>` list in the shared project file and rebuild.

## Prerequisites
- Follow the environment checklist (xref:Uno.GetStarted.vs2022).
- Validate tooling with `uno check` (xref:UnoCheck.UsingUnoCheck).

## Create a New MVUX App
- **Visual Studio 2022 (extension v4.8+)**
  - Create a new *Uno Platform App* (`Ctrl`+`Shift`+`N`).
  - Choose the *Blank* preset, open the **Presentation** tab, and select *MVUX*.
  - Finish the wizard; the generated solution includes MVUX packages and templates.
- **Command Line**
  ```cmd
  dotnet new unoapp -preset blank -presentation mvux -o MyApp
  ```
  - Open `MyApp.sln` in Visual Studio or VS Code to start building.

## Enable MVUX in an Existing App
- Applies to projects created with the Uno Single Project template (xref:Uno.Development.MigratingToSingleProject).
- Edit the shared `.csproj` and update `<UnoFeatures>`:
  ```xml
  <UnoFeatures>
      Material;
      Extensions;
      MVUX;
      Toolkit;
  </UnoFeatures>
  ```
- Rebuild the solution so the MVUX feature packages and generators activate.

## Next Steps
- Explore the MVUX overview (xref:Uno.Extensions.Mvux.Overview).
- Review Feed and State concepts before wiring data flows.
