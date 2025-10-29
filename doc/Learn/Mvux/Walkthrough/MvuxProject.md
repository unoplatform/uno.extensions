---
uid: Uno.Extensions.Mvux.HowToMvuxProject
---

# MVUX Project Setup

Quick instructions for starting a new MVUX-enabled Uno Platform app or turning MVUX on in an existing solution.

## Create a new MVUX app in Visual Studio

- Open *Uno Platform App* from the project templates (`Ctrl`+`Shift`+`N` → Uno Platform App).
- Choose the *Blank* preset in the wizard.
- On the **Presentation** tab, pick *MVUX*.
- Finish the wizard; the generated project ships with MVUX packages wired up.

![Selecting MVUX in the Uno extension wizard](../Assets/MvuxProject-Mvux.png)

## Create a new MVUX app with dotnet new

```cmd
dotnet new unoapp -preset blank -presentation mvux -o MyApp
```

- Open `MyApp.sln` in Visual Studio or VS Code to start building.

## Enable MVUX in an existing Uno project

- Ensure the project uses the Uno Single Project template (xref:Uno.Development.MigratingToSingleProject).
- Edit the shared `.csproj` and add `MVUX;` to `<UnoFeatures>`:

  ```xml
  <UnoFeatures>
      Material;
      Extensions;
      MVUX;
      Toolkit;
  </UnoFeatures>
  ```

- Rebuild so MVUX packages and generators activate.

## Resources

- Uno SDK feature reference: (xref:Uno.Features.Uno.Sdk)
- MVUX overview: (xref:Uno.Extensions.Mvux.Overview)
