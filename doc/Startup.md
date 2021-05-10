# Startup phases
_[TBD - Review and update this guidance]_

The startup of the application is divided into 3 different classes.

- **App**
  - This is the main entry point of the application (equivalent to `Program.cs`).
  - It contains platform specific application delegates (e.g. `OnLaunched`, `DidEnterBackground`, etc.).
  - It is located under `UWP/App.xaml.cs`.

- **Startup**
  - This is the UI startup. It will initialize any **UI related components**.
  - It is located under `Shared.Views/Startup.cs`.

- **CoreStartup**
  - As it name suggests, this is the core startup. It will initialize any **non-UI related components**.
  - It is located under `Shared/CoreStartup.cs`.

The startup is divided because not all startup will have access to UI components and we want to share as much initialization code as possible.

For example, unit tests only use the `CoreStartup`. This can also be true for apps that need to run background services or start from unusual entry points (e.g. iOS callkit).

## Sequence

The following diagram represents the startup sequence of the application.

There are 3 main phases.

- PreInitialize
- Initialize
- Start

```
       App                Startup               CoreStartup
      -----              ---------             -------------
        |                    |                       |
1: Constructor  --->   PreInitialize   --->    PreInitialize     (Runs on the UI thread)
        |                    |                       |
2: OnLaunched   --->     Initialize    --->     Initialize       (Runs on the UI thread)
        |                    |                       |
  Shell created <---   OnInitialized   <---    OnInitialized    
        |                    |                       |
3:    Start     --->       Start       --->        Start         (Runs on a background thread)
                             |                       |
                        StartServices           StartServices    (Both tasks are executed in parallel)
```

## PreInitialize

This phase is started when the `Windows.UI.Xaml.Application` constructor is called.

You would do things here that need to happen as soon as possible.
- Set the app orientation (otherwise your splash screen might be disoriented).
- Set the app localization (otherwise some texts might be localized in the default culture).

This phase should be as short as possible (< 50 ms).

## Initialize

This phase is started when `Windows.UI.Xaml.Application.OnLaunched` is called.

You would configure your application in this phase.
- Setup your IOC container (e.g. registering services).
- Configure static properties (e.g. default loggers).
- Register to static events (e.g. unhandled exceptions).

If you can't wait the start phase to configure specific components but need access to your registered services, you can use the `OnInitialized` method.

The completion of this phase means that the application is ready to be used by background services.

## Start

This phase is started once the initialization is complete. 

It is a `Task` executed on a **background thread**.

The two `StartServices` method are **executed in parallel**.

You would start your application in this phase.
- Execute the initial navigation.
- Start background services
  - Analytics tracking
  - Location tracking
  - User session (expiration, user inactivity)
  - Background transfers

This phase will complete once everything is started.

The completion of this phase means that the application is ready to be used by the user.