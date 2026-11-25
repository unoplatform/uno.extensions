# 027 - Aspire support for Uno apps

**Status: lots 1 to 4 in progress (code done; open items listed below), lots 5 and 6 not started.**

The design is extracted from a working sample: an Uno app on desktop, WebAssembly and Android, an
ASP.NET Core API and a PostgreSQL container under one AppHost. The sample is local and not in the
repository: it is where each head was run by hand, and the tests under *Tests* guard the
behavior in the repository. Anything not run on it is marked *unproven*.

Still open: running the published WebAssembly app, an AOT publish, and stopping a head on macOS
and Linux.

## Problem

An Uno app with a backend has no supported way to run the whole solution locally in one step.
Aspire does this for .NET, and three things stop an Uno app from taking part:

1. **The AppHost cannot add the app.** `AddProject` rejects a multi-target project ("Your project
   targets multiple frameworks"), so each head has to be an `AddExecutable` running
   `dotnet run -f <tfm>`.
2. **The app cannot use the service defaults.** The `aspire-servicedefaults` template depends on
   the ASP.NET Core shared framework, which an Uno head cannot reference. Without it the app has no
   service discovery, no resilience and no telemetry in the dashboard.
3. **The injected environment does not reach every head.** Aspire passes addresses as environment
   variables on the process it starts. A browser has no process environment, and an Android app
   does not inherit the one `dotnet run` had.

## Decision

Two packages:

| Package | Referenced by | Does |
|---|---|---|
| `Uno.Extensions.Aspire` | the Uno app | Service discovery, resilience and OpenTelemetry on the Uno.Extensions host; the build logic that reads a baked environment |
| `Uno.Extensions.Aspire.Hosting` | the AppHost | Registers the app's heads as Aspire resources and delivers their environment |

- The client package works with the `AddExecutable` workaround, so it ships first. The AppHost
  package needs it in the app for the heads whose environment is baked.
- The client half is a **package, not a project template** as Aspire's own service defaults are:
  an `UnoFeatures` entry needs a package, and the Kiota coordination below needs library code.
  Uno.Sdk's `Aspire` feature adds it, at the version of the other `Uno.Extensions` packages; with an
  SDK that predates the feature, the app references the package.
- The whole `Uno.Extensions.Aspire.Hosting` surface is marked `[Experimental]`. It stays so after
  the Android lot, until stopping a head is verified on macOS and Linux and telemetry questions 1
  and 2 are settled: the Android head also depends on a target of .NET for Android that is not
  public (see *Android emulator*).
- Its dependency on `Aspire.Hosting` is 13.6.5 or later, with no upper bound. Earlier versions carry
  a security advisory, and a range would refuse each new major on release, as 17 followed 13. The
  package builds against 13.6.5, and its tests have run on 13.6.5 and 17; CI runs them on 17.

## Client package: `Uno.Extensions.Aspire`

`net10.0`, no UI dependency, so every head references the same assembly. `IsAotCompatible` is set
and the package builds with no trim or AOT warning and no suppression.

```csharp
this.CreateBuilder(args)
    .Configure(host => host
        .UseConfiguration(configure: config => config.EmbeddedSource<App>())
        .UseServiceDefaults()
        .UseHttp((context, services) => services.AddKiotaClient<TasksApiClient>(context, name: "TasksApi")));
```

`UseServiceDefaults()` is an `IHostBuilder` extension in namespace `Uno.Extensions`, named `Use*`
like the rest of Uno.Extensions hosting. An overload takes `ServiceDefaultsOptions`: Aspire's own
service defaults are a project the app edits, which a package cannot be, so the options turn the
resilience handler or the telemetry off, and change the resilience handler after unsafe methods
are excluded from retries. Every call changes the same options, in call order, and the parts are
added when the host is built: a library can call `UseServiceDefaults()` and the app change a choice
afterwards. `ConfigureResilience` is a delegate like any other property: assigning it replaces an
earlier caller's, and `+=` adds to it.

### Service discovery

`AddServiceDiscovery()` on the container and on every `HttpClient`
(`ConfigureHttpClientDefaults`). An endpoint section keeps a logical address:

```json
{ "TasksApi": { "Url": "https+http://api" } }
```

`AddClientWithEndpoint` sets it as `BaseAddress` unchanged, and the resolver replaces the host per
request from `services:api:http:0`, which Aspire injects as `services__api__http__0`.

**Injected addresses win.** The Uno host adds environment variables first and the app's embedded
`appsettings*.json` after, and the later source wins. So `UseServiceDefaults` adds one more source
holding the `services__*` environment variables only, after every other source. It registers that
source from a host configuration step, which runs before the app configuration steps are walked,
so the source lands last whatever the order of `UseServiceDefaults` and `UseConfiguration`.

- An address Aspire passes wins over the same key in a settings file. Every other key keeps the
  usual precedence.
- A `services` section in a settings file is a fallback: it is used when nothing is injected, as
  when the app runs without the AppHost.
- The override is per key: a settings file naming `services:api:https:0` while Aspire injects only
  `services:api:http:0` keeps both, and `https+http` then picks the settings' `https` address.
- A literal `Url` in a settings file is still used as is, since the resolver never sees it. Apps
  that run under Aspire keep the logical form in every settings file.
- Names are compared ignoring case: an Android app receives them in upper case.

### Resilience

`AddStandardResilienceHandler` on every `HttpClient`, with retries disabled for unsafe methods
(`Retry.DisableForUnsafeHttpMethods()`): a retried `POST` after a timed-out insert creates a
duplicate.

- **Defaults apply to every client from the factory**, including third-party and authentication
  clients: 10 s per attempt, 30 s in total, 3 retries, a circuit breaker. A long upload, a
  download or a streamed response exceeds them.
- **Per-client opt-out** is the standard `RemoveAllResilienceHandlers()`, passed through the
  `configure` callback of `AddClient` / `AddKiotaClient`. It is still marked experimental
  (`EXTEXP0001`), so the app suppresses that diagnostic where it calls it. The how-to shows it.
- **Handler order.** The resilience and discovery handlers sit outside the handlers Uno.Extensions
  resolves from the container (authentication, diagnostics), so those run once per attempt.

**One retry layer with Kiota.** `AddKiotaClient` attaches Kiota's default handlers, including its
own `RetryHandler`, which runs inside the resilience handler. Measured with both layers on: a
failing `POST` is sent four times, because Kiota retries unsafe methods and the rule above only
governs the outer layer. `RetryHandler` falls back to a `RetryHandlerOption` from the container,
so:

- `UseServiceDefaults` sets a key in the host builder's `Properties` when it is called with the
  resilience handler on. With it off, Kiota keeps its own retries.
- The key means "`UseServiceDefaults` added the resilience handler", not "a resilience handler is
  present": an app that adds its own standard resilience handler still stacks Kiota's retries.
- The key is read across separately versioned packages. It is a constant, so its value is compiled
  into `Uno.Extensions.Http.Kiota` and no internal method is called across the two. An older Kiota
  package ignores it, so the packages are used at the same version.
- `AddKiotaClient` reads the key from the `Properties` of the `HostBuilderContext` it already
  receives. When set, it registers a no-retry `RetryHandlerOption` with `TryAddSingleton`.

Consequences:

- An app that never calls `UseServiceDefaults` gets no change: no option is registered.
- The key is written when `UseServiceDefaults` is called and read when the container is built, so
  the order of `UseServiceDefaults` and `UseHttp` does not matter.
- An app that registers its own `RetryHandlerOption` keeps it.
- A `RetryHandlerOption` passed on a single request still wins inside Kiota and brings the second
  layer back for that request.
- A Kiota client that removes the resilience handler has no retries unless the app registers an
  option for it.

No new public type is added to `Uno.Extensions.Http` or `Uno.Extensions.Http.Kiota`.

### Telemetry

OpenTelemetry is registered **only** when `OTEL_EXPORTER_OTLP_ENDPOINT` is present in
configuration, the same gate as the server template, **and** the host environment is Development.
An app started without Aspire registers no exporter, no instrumentation and no hosted service.

- **A released app exports nothing by default.** The endpoint can appear in an app's configuration
  on someone else's machine, as an environment variable. `ExportTelemetryInAllEnvironments` opts a
  released app in.
- The AppHost sets `DOTNET_ENVIRONMENT=Development` on every head, as Aspire does for its projects,
  and Uno's default host reads it. An app that sets its own environment keeps it.

When the gate is open: logs (formatted message and scopes), metrics (HttpClient and runtime) and
traces (HttpClient), exported with `UseOtlpExporter()`. The service name comes from the
`OTEL_SERVICE_NAME` Aspire injects, so each head appears under its resource name.

- **Never on WebAssembly.** The gate also requires `!OperatingSystem.IsBrowser()`, written so the
  linker removes the OpenTelemetry code from a `browserwasm` build. Confirmed: a trimmed publish
  contains no OpenTelemetry assembly.
- **An unreachable collector never fails or delays a request.** Export is batched off the request
  path.
- **Flushing.** The last batch is sent when the host stops. With the collector unreachable,
  stopping the host took about nine seconds in a test, so an app never stops or disposes its host
  synchronously on the UI thread. A killed process loses its last batch. The how-to does not yet
  show a window-close flush: a bounded one has not been tried on a real app.
- **Request headers reach the dashboard.** `DiagnosticHandler` logs every request header at
  Information, and those logs are exported when the gate is open. Lot 1 logs the path without the
  query string, and a header value only when it is known to carry no credential: `Accept`,
  `Content-Type`, `User-Agent` and `traceparent` as they are, `Location`, `Content-Location` and
  `Referer` without their query string, every other value redacted, as `HttpClientFactory`
  logging does since .NET 9. A deny-list of names would miss `Signature` or `X-Session-Id`. This
  changes the log output of a published package, so the release notes say so.

### Dependencies

Added to `Directory.Packages.props`, referenced by `Uno.Extensions.Aspire` only:
`Microsoft.Extensions.ServiceDiscovery`, `Microsoft.Extensions.Http.Resilience`,
`OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Exporter.OpenTelemetryProtocol`,
`OpenTelemetry.Instrumentation.Http`, `OpenTelemetry.Instrumentation.Runtime`.

- Central transitive pinning turns each version into a floor for every app. Lot 1 uses the
  versions the sample ran on (resilience and service discovery 10.10.0, OpenTelemetry 1.20.1).
  They restore with the repository's `Microsoft.Extensions.*` 10.0.12 unchanged, and bring no
  gRPC or protobuf package. An app on the current Uno.Sdk restoring without a downgrade error is
  not checked yet.
- OpenTelemetry is 1.15.2 or later, the first release whose batch workers run without threads.
- **WebAssembly size gates the first stable release.** On a trimmed Release publish of the sample,
  the package adds 0.17 MB Brotli-compressed and no OpenTelemetry assembly. Removing a dependency
  after a stable release is breaking, so the decision to exclude one for `browserwasm` is made
  then.

## AppHost package: `Uno.Extensions.Aspire.Hosting`

`net10.0`, references `Aspire.Hosting`. Never referenced by an app head, and it needs no
Uno.Extensions code.

```csharp
var app = builder.AddUnoProject("app", "../MyApp/MyApp.csproj");

app.AddDesktop().WithReference(api).WaitFor(api);
app.AddWebAssembly().WithReference(api).WaitFor(api);
app.AddAndroidEmulator().WithReference(api).WaitFor(api);
```

| Method | Returns |
|---|---|
| `AddUnoProject(string name, string projectPath)` | `IResourceBuilder<UnoProjectResource>` |
| `AddDesktop(string? name = null)` | `IResourceBuilder<UnoDesktopResource>` |
| `AddWebAssembly(string? name = null)` | `IResourceBuilder<UnoWebAssemblyResource>` |
| `AddAndroidEmulator(string? name = null)` | `IResourceBuilder<UnoAndroidEmulatorResource>` |
| `WithTargetFramework(string tfm)` | the head it was called on |
| `WithLaunchProfile(string profileName)` | the desktop head it was called on |
| `WithClientEnvironment(string name)` | the WebAssembly or Android head it was called on |

- `WithLaunchProfile` takes the desktop head only: a profile's `applicationUrl` overrides the port
  the AppHost serves the WebAssembly head on.
- `WithClientEnvironment` has one overload per head whose environment is written into the app,
  WebAssembly and Android. It is not on the base type, where it would do nothing for the desktop
  head, and it is not turned into a generic method later, which would break binary compatibility.
  Its name does not say how the environment reaches the app: for WebAssembly, delivery at run time
  can replace build time without another rename (see *Alternatives considered*).

- `AddUnoProject` holds the project path and starts no process. It is not a resource in the model,
  so only heads appear in the dashboard. A project file that does not exist fails the AppHost at
  that call. The AppHost has no `ProjectReference` to the app.
- The names follow Aspire's MAUI integration, `AddMauiProject` and `MauiProjectResource`, which
  has the same shape: a project path that starts no process, with one resource per platform.
- The API is in the `Aspire.Hosting` namespace, as Aspire integrations are, and is marked
  experimental under the diagnostic id `UNOASPIRE001`.
- Default head names are `<app>-desktop`, `<app>-wasm` and `<app>-android`.
- References are declared per head, because the address a head can use differs per head.

### How a head runs

| Head | Process | Environment delivery |
|---|---|---|
| Desktop | `dotnet run --project <path> -f <tfm> --no-build --no-launch-profile` in the project folder | Process environment |
| WebAssembly | the same, plus an HTTP endpoint for the served app | Baked into the bootstrapper at build time |
| Android emulator | the same, plus `-p:AdbTarget=-e` | Baked into the APK at build time |

- **Target framework.** The one of the head's platform in the project file's `TargetFramework` or
  `TargetFrameworks`, read without evaluating the project. When the file names none or computes
  them from properties, the AppHost's .NET version with the platform (`net10.0-desktop`,
  `net10.0-browserwasm`, `net10.0-android`). `WithTargetFramework` overrides it. A start fails with
  a message naming the project's frameworks when they do not include the head's.
  - A platform with a version, such as `net10.0-android36.0`, matches its platform.
  - A group that appends to the list, `$(TargetFrameworks);net10.0-ios`, adds its own values. Any
    other property makes the list unknown, and no start is refused on it.
- **Launch profile.** None by default. Without `--no-launch-profile`, `dotnet run` takes the first
  profile, which may belong to another head. `WithLaunchProfile` opts in; a profile's own
  `environmentVariables` then override what Aspire injects.
- **Build.** Heads share one project and its `obj` folder, and two builds of it at once fail. So
  before a head starts, the integration builds it under a lock held per app: the head shows as
  *Queued* while another head of the app builds, then *Building* while `dotnet build -f <tfm>` runs,
  with the build output in its log. It then runs with `--no-build`, which still deploys an Android
  app, and the lock covers the build only. The arguments given with `WithArgs` that change what
  the build produces or where are passed to the build too, since `--no-build` would otherwise drop
  them: properties, configuration, runtime, architecture and OS, joined or separate, and nothing
  after `--`. A head given a Release configuration gets no address written into it. The build gets
  none of the head's environment, so no
  value reaches a binary log. A build fails the head after ten minutes. Builds an IDE makes are not
  serialized with these. This follows the build queue of Aspire's own MAUI integration, which holds
  its lock until an Android app ends.
- **Start.** The desktop head starts with the AppHost. The WebAssembly and Android heads start from
  the dashboard, so a run does not build every head, and an Android head needs an emulator.
  Aspire's own `WithExplicitStart()` turns automatic start off for the desktop head.
- **Stop.** Stopping a head ends the app, not only `dotnet run`, whether the head or the whole
  AppHost is stopped. For the Android head, the app on the emulator ends too. No code of the integration does this: it is Aspire's process handling,
  verified on Windows and *unproven* on macOS and Linux.
- **Publish.** In publish mode the project file is not checked, so a publish runs where the app
  is not checked out.
- **Not published.** A head is left out of the deployment manifest.
- **Failure.** A failed build stops the head, with the build output in its console log.

### Hot Reload

Every head connects to a dev server, with or without an IDE. The `uno.devserver` CLI does the
work; the AppHost only runs it and reads the result.

- **Which CLI.** The version of the `Uno.WinUI.DevServer` package the app restores, which the
  Uno.Sdk chooses: the CLI is released with the same version, and another version may not know the
  options or the dev server of that release. Before any head builds, the AppHost restores the app,
  under its build lock, and reads the version from the assets file the restore names. The restore
  is a no-op once done, and keeps the version in step with an Uno.Sdk update or another `obj` folder.
  A value that is not a package version is not passed to the CLI.
- **Where.** In the first folder above the app's project that holds a solution: the CLI looks for a
  solution in its working folder only, and a dev server serves a solution. An app in no solution
  gets none.
- **Reuse or start.** Before any resource starts, the AppHost runs the CLI's `start` command with
  the AppHost as the parent process. The CLI reuses the dev server already running for the
  solution, an IDE's for example, so the IDE's tools see the heads, and replaces one whose parent
  process is gone. Otherwise it starts one that stops with the AppHost, even a killed one. The
  AppHost does not wait for the CLI, which may return or stay to relay the dev server's output: it
  reads the port from `uno.devserver disco --json`, retrying for two minutes while the dev server
  registers.
- **Stop.** The AppHost never stops a dev server itself. One it started notices the AppHost is gone,
  stops within seconds and removes its registration; one it reused keeps running.
- **In the dashboard** it is `uno-devserver`: *Running*, with its port, its process and who started
  it; *Unavailable* or *FailedToStart*, with the reason and how to turn it off; or *Exited*, when it
  stops during the run. One serves every app of a solution; an app in another solution gets
  `uno-devserver-2`, and so on. A head shows *Waiting for dev server* until the port is known.
- **Port.** A head's build gets `-p:UnoRemoteControlPort=<port>`, the property an IDE sets through
  `.csproj.user`. The Uno.Sdk turns it into the addresses the app connects to, for every head. It
  overrides a value set in the project, and it is the first build argument, so a port given with
  `WithArgs` wins. A head waits up to 15 minutes for the port, then builds without one, and that run
  has no Hot Reload.
- **Off.** The AppHost setting `Uno:DevServer:Enabled` set to `false`
  (`Uno__DevServer__Enabled=false` as an environment variable) adds no dev server: heads are built as
  before, with the port an IDE wrote, if any. A value other than `true` or `false` stops the AppHost.
  Use it offline, in CI, or to keep a dev server from listening: it accepts connections on every
  network interface, as one an IDE starts does, and on Windows a first start can ask to add a
  firewall rule.
- `DOTNET_MODIFIABLE_ASSEMBLIES=debug` is set on every head: `dotnet run --no-launch-profile` does
  not set it.
- An IDE opened after the AppHost starts a dev server of its own: the heads keep the AppHost's for
  that run. A dev server the AppHost started stops with it, even if an IDE uses it meanwhile.
- A CLI that cannot start the dev server, or whose output the AppHost does not recognize, leaves the
  dev server *Unavailable* with its version in the reason.
- The CLI relays the output of a dev server it starts while it runs: the dev server's later log
  lines are not in the dashboard.

Verified on a generated `-preset recommended -server -aspire` app with desktop and WebAssembly
heads, never built before: the AppHost restored it, the CLI started the dev server, both heads
connected, a XAML edit reached the desktop window and the browser, and when only the AppHost was
ended, the dev server stopped within seconds and removed its registration. With a dev server
already running for the solution, the heads used it and it survived the AppHost. With the setting
off, no dev server was added.

### Desktop

Receives its environment like any process. Gets `WithOtlpExporter()` so the dashboard endpoint and
service name are injected.

When no dev server is available, the Hot Reload client's failed connection attempts appear in the
dashboard as failed requests of the head.

### Baking the environment (WebAssembly and Android)

Proven for WebAssembly in lot 3 and for the Android emulator in lot 4.

Before a head is built, the integration resolves its environment and writes a **values file** into
the app's own `obj` folder, `obj/uno-aspire/<target framework>.v1.values.proj`. It is a small
project that returns name and value pairs, the same for every platform.

The client package `Uno.Extensions.Aspire` ships build logic (`buildTransitive`) that calls it
when it exists and turns the pairs into what the platform understands: `WasmShellMonoEnvironment`
items for WebAssembly; for Android, the `RuntimeEnvironmentVariable` items `dotnet run -e` adds,
added before .NET for Android's `_GenerateEnvironmentFiles`, which writes them into the app's own
environment file. Nothing is passed on the command line, so every build of the app finds it: the
head's own, and the one an IDE makes while the AppHost runs.

**Alternatives considered.**

- **A command-line property**, as Aspire's MAUI integration passes a targets file to `dotnet run`:
  a build the IDE makes never sees it. The first version of this design did so, and an IDE build of
  the WebAssembly target removed the address while the head ran.
- **An imported file holding the values:** a changed value changes an import, and the app is
  compiled again. The values file is called with the `MSBuild` task instead.
- **Delivery at run time through a gateway**, as Aspire's Blazor integration does: the page reads
  its configuration from the AppHost before it boots, and APIs and telemetry are proxied from the
  same origin. No CORS, no file left behind, telemetry possible in the browser. It needs a hook in
  `Uno.Wasm.Bootstrap` that runs before the app boots, which does not exist yet; the public name
  `WithClientEnvironment` leaves the door open.
- **A push over the Uno dev server's channel:** no rebuild and nothing in the build output, but
  the addresses arrive after startup, and it requires a running dev server.

A consequence: a WebAssembly or Android app started from the IDE gets the addresses only once its
head has been started from the dashboard in this AppHost run.

What depends on the head kind is decided by the AppHost, which knows it: the allow-list below,
and for Android the names in upper case, the loopback rewrite and leaving out `https`. The build
logic only maps.

The file is valid while the AppHost runs:

- It is written at every start of the head, before its build, never in publish mode. When the
  environment fails to resolve, the start fails and the last file stays: a partial environment
  would overwrite good values. Whether a value changed in the dashboard during a run reaches the
  next start is not measured.
- Stopping the head alone leaves it: the services it points to still run.
- The AppHost removes it when it stops and again when it starts, and renews it every minute in
  between. A killed AppHost, which is what an IDE's stop does, never reaches its stop. So a Debug
  build removes a file not renewed for five minutes, or dated in the future, with warning
  `UNOASPIRE103`, rather than bake addresses nobody serves. Removing it means a build that treats
  warnings as errors fails once, not every time. The age is measured in UTC. The build output lists
  the names it read.
- The renewal and the lease are part of the file's format: changing either changes the version in
  its name. A test checks that several renewals fit in the lease.
- A changed value reaches an incremental build. The WebAssembly bootstrapper writes its
  configuration and .NET for Android its environment file on every build, and a redeployed Android
  app carries the new file. Checked by hand: a changed port, rebuilt and redeployed with no clean.
- A removal that fails is a warning in the AppHost's own log, which outlives the dashboard.

Consequences:

- The app's csproj names no resource, but the app has to reference `Uno.Extensions.Aspire`.
  Without it the head runs and the app has no address. After the head's build, which restores, the
  AppHost reads the libraries of the app's `obj/project.assets.json`, with a source-generated
  serializer, and warns if the package is not among them: a reference that depends on a property
  passed to the head is checked on the start that changes it. With another intermediate output
  path there is no such file and nothing is checked.
- The path is the app's project folder, so the projects the app references, which have their own
  folder, read nothing.
- The AppHost and the client package agree on the path and the format of the file. The format
  version is in the file name: a client that finds only another format's file warns
  (`UNOASPIRE101`) and reads nothing. Packages of different versions that write and read the same
  format work together; the warning asks for the same version of both, which always agree.
- Two heads of the same app for the same target framework share one file.
- Two AppHosts running the same app remove each other's file. A head whose target framework
  changed after an AppHost was killed leaves the old framework's file until it is deleted.

**What may be baked is an allow-list.** A baked value is readable by anyone who loads the page or
unpacks the APK.

- Baked: `services__*` variables whose value is an endpoint of a resource in the model.
- Everything else, including connection strings, parameters, literal values and `OTEL_*`, is left
  out. It does not stop the head: Aspire itself adds a variable such as `API_HTTP` to every
  reference, so refusing unknown names would stop every head.
- The AppHost opts a single variable in by name with `WithClientEnvironment`, whatever its value.
- The check follows a value into the expressions it is built from, so an endpoint wrapped around a
  secret parameter is left out.

The head's log says what happened, with names and never values:

- Information: the names written into the app.
- Information: for Android, the service addresses left out over `https` on the host's loopback.
- Warning: a `services__*` variable left out, because the app will not reach that service. It
  names `WithClientEnvironment` as the way to write one anyway.
- Warning: for Android, a service with no address left once those are out.
- Warning: a name opted in that the head does not have, or one whose value holds a secret.
- Debug: the other names left out.

The build logic's warnings have their own codes, `UNOASPIRE101`, `UNOASPIRE102` and `UNOASPIRE103`, so a build that
treats warnings as errors can silence one without silencing the experimental API's `UNOASPIRE001`.

**No baked value reaches a shipped artifact.**

- The build logic reads the file only in the `Debug` configuration, not optimized and not a
  publish (`_IsPublishing`). Any other build regenerates its bootstrapper configuration without
  the values. A `-t:Publish` through MSBuild with `-p:Configuration=Debug` does not set
  `_IsPublishing` and is not covered.
- A value that changes between two AppHost runs reaches the next build.
- **Not covered:** `dotnet publish --no-build` after a Debug build copies the bootstrapper
  configuration as built, address included. Nothing runs without a build. The how-to says to
  publish with a build.

**Incremental builds stay incremental.** The file is not imported, so a changed value does not
compile the app again. It is written only when its content changes, with variables in a fixed order,
through a temporary file, unique per write, so a build never reads half of one. A write that
fails stops the head with a message naming it and the path; a temporary file it leaves is removed
with the values.

**The files are written, not concatenated.** XML is produced by an XML writer, values are escaped
for MSBuild, and a value containing a line break or a name outside `[A-Za-z_][A-Za-z0-9_]*` is
refused. Android names are written in upper case, because the Android runtime drops lowercase ones;
configuration keys are case-insensitive, so the app reads them unchanged.

### WebAssembly

- The integration owns the port the app is served on: the head has an endpoint named `http`
  (`UnoWebAssemblyResource.EndpointName`), and its target port is passed to the dev server as
  `ASPNETCORE_URLS`. The app's launch settings are not read.
- The head has an HTTP health check on `/`: healthy once the app is served.
- The served endpoint is local, not external.
- The dev server's own variables (`ASPNETCORE_URLS`, `DOTNET_MODIFIABLE_ASSEMBLIES`) stay in its
  process and are not baked.
- No `WithOtlpExporter()`: the dashboard key would be public in the page. See *Open questions*.
- The browser calls the API directly, so the API needs a CORS policy. The how-to restricts it to
  the WebAssembly head's origin, passed to the API by the AppHost, not to any origin.

### Android emulator

Implemented in lot 4. The sample first ran this head through a preview integration built for
another framework, with the address set by hand; a stable package cannot depend on that.

- The head runs `dotnet run -f net10.0-android --no-build -p:AdbTarget=-e`, which deploys to the
  running emulator and stays attached to the app.
- An emulator reaches the host machine at `10.0.2.2`. A service address on the host's loopback
  is written with that host, over **`http` only**: over `https` it would present a development
  certificate the emulator does not trust, so it is left out and named in the head's log. A
  service left with no address is a warning. An `https` address that is not on the loopback, a
  dev tunnel for example, is written unchanged.
- Every `services__*` variable is treated as a service address, whether it passed the allow-list
  or was opted in. Other opted-in variables keep their value. Names are written in upper case; two names that
  differ only by case stop the head.
- The how-to allows cleartext traffic to `10.0.2.2` only, through a network security
  configuration, and does not set `usesCleartextTraffic` for the app. The configuration is in
  every build, Release included. On a device `10.0.2.2` is an ordinary private address that can
  belong to another machine; a Release build is never configured to call it, since the AppHost
  writes nothing into one.
- The build logic hooks a target and an item of .NET for Android that are not public, the hook
  Aspire's MAUI integration relies on too. The item is read by the Android workload 36.1.118, not
  by 36.1.2. After the build it checks that the values reached the environment file, and warns
  (`UNOASPIRE102`) naming them when they did not, for an older workload or a renamed target.
- An API that redirects `http` to `https` (`UseHttpsRedirection`) sends the emulator to the
  untrusted certificate. The how-to says so.
- No `WithOtlpExporter()`: see *Open questions*.
- The head needs exactly one running emulator: with `-p:AdbTarget=-e`, adb targets the only
  emulator and ignores attached devices. Booting one is out of scope.

## Out of scope

- iOS, physical Android devices and WinAppSDK heads. A device needs the API behind an
  authenticated tunnel.
- Publishing or deploying an Uno head through Aspire. This is the local development loop only.
- Booting an emulator from the dashboard.
- A server-side service defaults project: the Aspire template covers it.
- Evaluating the project to find its target frameworks.

## Open questions

1. **WebAssembly telemetry.** OTLP over gRPC does not run in a browser. It needs `http/protobuf`,
   CORS on the dashboard's OTLP endpoint, and a way to keep the dashboard key out of the page.
2. **Android telemetry.** The dashboard's OTLP endpoint is on the host's loopback. The sample
   reached it through a dev tunnel that needs a signed-in user. Until settled, the Android head
   gets no `WithOtlpExporter()`.

## Lots

Dependency-ordered. Lots 1 to 4 go out as three pull requests, cut by what a reviewer checks
rather than by head:

- **Client package**: lot 1.
- **AppHost package**: lots 2, 3 and 4 without the baking, so the three heads run as processes
  and the WebAssembly and Android apps have no address yet.
- **Baking the environment**: the rest of lots 3 and 4, the values file and the client package's
  build logic that reads it.

Later lots are one pull request each.

1. **Client package.** `Uno.Extensions.Aspire`; the `Properties` key read by `AddKiotaClient`;
   header redaction in `DiagnosticHandler`; the size measurement. The how-to under `doc/Learn/`
   ships with the last of the three pull requests, once it can show the whole feature.
2. **AppHost package, desktop.** `AddUnoProject`, `AddDesktop`, `WithTargetFramework`,
   `WithLaunchProfile`, the start and stop behavior.
3. **AppHost package, WebAssembly.** Opens with the baking spike. `AddWebAssembly`, the values
   file and the client package's build logic that reads it, the allow-list.
4. **AppHost package, Android emulator.** `AddAndroidEmulator`, the `http`-only rewrite.
5. **Feature and template.** An `Aspire` entry in `UnoFeatures` and a template option. Both live
   in other repositories and follow the packages.
6. **Telemetry on WebAssembly and Android**, if questions 1 and 2 resolve to doing it.

Lots 1 and 2 are enough to run an app's desktop head with its backend in one step.

## Tests

- `Uno.Extensions.Aspire.Tests` runs `UseServiceDefaults` on a host with stub handlers and a
  loopback server: which address a request goes to, how many attempts it makes with and without
  Kiota, what is exported, and what `DiagnosticHandler` logs.
- `Uno.Extensions.Aspire.UI.Tests`, a runtime test, checks that the injected address wins on Uno's
  own host builder, the one apps get, before and after the settings are added.
- `Uno.Extensions.Aspire.Hosting.Tests` works on the application model with endpoints allocated by
  the test: run arguments and environment, target frameworks, the build lock with a real
  `dotnet build` of a project that builds instantly, what is written into the app and logged, and
  when the file is removed.
- `Given_AppBuildLogic`, in the same project, runs the client package's build logic with MSBuild in
  a process, on a project that stands in for the app, against the file the AppHost wrote.
