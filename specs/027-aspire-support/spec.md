# 027 - Aspire support for Uno apps

**Status: lots 1 to 4 in progress (code done; open items listed below), lots 5 and 6 not started.** The design is extracted from a working
sample (an Uno app on desktop, WebAssembly and Android, an ASP.NET Core API and a PostgreSQL
container under one AppHost). Anything the sample did not exercise is marked *unproven* and names
the lot that proves it.

Verified on the sample's desktop head, with the package in place of its hand-written project and
of its hand-registered Kiota option: started by the AppHost, the app resolved `https+http://api` to
the injected address, its call went through the resilience handler, and the dashboard showed one
trace of three spans (the app's request, the API's, the database query). A task added from the
app's own UI reached the API as one `POST` and the database as one row.

WebAssembly size, measured on a trimmed Release publish of the sample with and without the
package: 14 more assemblies, 0.47 MB more uncompressed (46.10 MB against 45.64 MB) and 0.17 MB more
Brotli-compressed (12.55 MB against 12.38 MB). No OpenTelemetry assembly is in the output; the
added ones are the resilience and service discovery libraries.

Still open in lot 1: running the published WebAssembly app, and an AOT publish.

Lot 2, verified on the same sample with `AddUnoApp(...).AddDesktop()` in place of `AddExecutable`:
the head started with the AppHost once the API was healthy, and its call reached the API and the
database. Stopping the head alone ended the app process, starting it again started the app again, and
stopping the AppHost ended it.

Lot 3, verified on the same sample with `AddWebAssembly()` next to the desktop head and nothing
about Aspire in the app's project: the head stayed stopped until started from the AppHost, its
build wrote the API's address into the bootstrapper configuration, the app loaded from the
address the AppHost owns, and a task added in the browser reached the API as one `POST` after a
CORS check the API answered for that origin only. Stopping the head ended the dev server.
Nothing was written before the head was started, and the head reported unhealthy while it built
and healthy once the app was served.

With the values in the app's `obj` folder: a `dotnet build` of the WebAssembly target while the
head ran, as an IDE makes, said it used them, kept the address in the bootstrapper configuration,
and the browser still reached the API. Stopping the head alone left the file; stopping the
AppHost removed it, and the next build carried no address. With an `https` endpoint added to
the API, both addresses were baked and the browser called the `https` one, with the development
certificate trusted.

Measured while settling the lot 3 review:

- A quote and a backslash in a baked value reach the page escaped: the bootstrapper writes
  values as JavaScript strings.
- A changed value in an imported file compiled the app again. With the values in a project the build
  calls instead, a changed value reached the page and the app was not compiled again.
- A launch profile's `applicationUrl` overrode the port passed to the WebAssembly dev server.
- With the first design, a targets file passed as a property to the head's build only, a build
  of the WebAssembly target without the property, as an IDE makes, removed the baked address.
  That is why the values now live in the app's `obj` folder.
- `dotnet publish -c Debug --no-build` after an AppHost build copied the baked address into the
  publish output.
- Restarting the head from the dashboard wrote the values file again, after it had been deleted by
  hand: the head's arguments are resolved at every start.
- One MSBuild build manager and project collection kept across builds, as an IDE keeps them, read
  each rewrite of the values file: no stale value from a cached project.

Still open in lot 3: stopping a head on macOS and Linux, and two heads building the same project
at once.

Lot 4, verified on the same sample with `AddAndroidEmulator()` on an x86_64 emulator, API 35:

- The head built and deployed the app; `dotnet run` stayed attached and streamed the app's log
  into the head's console.
- The app's environment carried `SERVICES__API__HTTP__0=http://10.0.2.2:5171`. A task added on the
  emulator reached the API, and the API returned it to the host.
- An `https` endpoint of the API was left out with an Information line naming it.
- The app reached `10.0.2.2` over plain `http` with a network security configuration that allows
  that address only, without `usesCleartextTraffic`.
- A `dotnet build -f net10.0-android` while the head ran, as an IDE makes, kept the address;
  after the AppHost stopped, the next build's environment had none.
- Stopping the head ended the app on the emulator.

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
- The whole `Uno.Extensions.Aspire.Hosting` surface is marked `[Experimental]`. It stays so after
  the Android lot, until stopping a head is verified on macOS and Linux and telemetry questions 1
  and 2 are settled: the Android head also depends on a target of .NET for Android that is not
  public (see *Android emulator*). It tracks `Aspire.Hosting` 13.x and states that range in its dependency, from 13.6.5: earlier
  versions carry a security advisory.

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
like the rest of Uno.Extensions hosting. It takes no parameters: every piece below has a standard
per-client or per-app override, and an options overload can be added later without breaking.

### Service discovery

`AddServiceDiscovery()` on the container and on every `HttpClient`
(`ConfigureHttpClientDefaults`). An endpoint section keeps a logical address:

```json
{ "TasksApi": { "Url": "https+http://api" } }
```

`AddClientWithEndpoint` sets it as `BaseAddress` unchanged, and the resolver replaces the host per
request from `services:api:http:0`, which Aspire injects as `services__api__http__0`.

**Embedded settings beat the environment.** The Uno host adds environment variables first and the
app's embedded `appsettings*.json` after, and the later source wins. Two rules follow:

- A literal `Url` in any embedded settings file is used as is. The injected address is ignored and
  nothing reports it. Apps that run under Aspire keep the logical form in every settings file.
- A `services:<name>` section in embedded settings overrides what Aspire injects, so it cannot
  serve as a fallback. To run the app without the AppHost, set `services__api__http__0` in the
  environment.

`UseServiceDefaults` does not reorder configuration sources: that would change precedence for
every key in the app.

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

- `UseServiceDefaults` sets a key in the host builder's `Properties` when it is called, the
  existing `IsRegistered` pattern.
- The key means "`UseServiceDefaults` was called", not "a resilience handler is present": an app
  that adds its own standard resilience handler still stacks Kiota's retries.
- The key is read across separately versioned packages. An `Uno.Extensions.Http.Kiota` older than
  this change ignores it, so the packages are used at the same version.
- `AddKiotaClient` reads the key from the `HostBuilderContext` it already receives. When set, it
  registers a no-retry `RetryHandlerOption` with `TryAddSingleton`.

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
configuration, the same gate as the server template. An app started without Aspire registers no
exporter, no instrumentation and no hosted service.

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
  Information, `Authorization` included, and those logs are exported when the gate is open. Lot 1
  logs the path without the query string there, and redacts the value of any header whose name
  contains `auth`, `cookie`, `key`, `token` or `secret`.
- **The gate is the same in a Release build.** A shipped app exports when the variable is set on
  the machine or in a settings file. The how-to warns about it; a development-only gate is an open
  decision.

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
- **WebAssembly size gates the first stable release.** Lot 1 measures the Release `browserwasm`
  size with and without the package and records it in the pull request. Removing a dependency
  after a stable release is breaking, so the decision to exclude one for `browserwasm` is made
  then.

## AppHost package: `Uno.Extensions.Aspire.Hosting`

`net10.0`, references `Aspire.Hosting`. Never referenced by an app head, and it needs no
Uno.Extensions code.

```csharp
var app = builder.AddUnoApp("app", "../MyApp/MyApp.csproj");

app.AddDesktop().WithReference(api).WaitFor(api);
app.AddWebAssembly().WithReference(api).WaitFor(api);
app.AddAndroidEmulator().WithReference(api).WaitFor(api);
```

| Method | Returns |
|---|---|
| `AddUnoApp(string name, string projectPath)` | `IResourceBuilder<UnoAppResource>` |
| `AddDesktop(string? name = null)` | `IResourceBuilder<UnoDesktopResource>` |
| `AddWebAssembly(string? name = null)` | `IResourceBuilder<UnoWebAssemblyResource>` |
| `AddAndroidEmulator(string? name = null)` | `IResourceBuilder<UnoAndroidEmulatorResource>` |
| `WithTargetFramework(string tfm)` | the head it was called on |
| `WithLaunchProfile(string profileName)` | the desktop head it was called on |
| `WithBakedEnvironment(string name)` | the WebAssembly or Android head it was called on |

- `WithLaunchProfile` takes the desktop head only: a profile's `applicationUrl` overrides the port
  the AppHost serves the WebAssembly head on.
- `WithBakedEnvironment` has one overload per head that bakes its environment, WebAssembly and
  Android. It is not on the base type, where it would do nothing for the desktop
  head, and it is not turned into a generic method later, which would break binary
  compatibility.

- `AddUnoApp` holds the project path and starts no process. It is not a resource in the model,
  so only heads appear in the dashboard. A project file that does not exist fails the AppHost at
  that call. The AppHost has no `ProjectReference` to the app.
- The API is in the `Aspire.Hosting` namespace, as Aspire integrations are, and is marked
  experimental under the diagnostic id `UNOASPIRE001`.
- Default head names are `<app>-desktop`, `<app>-wasm` and `<app>-android`.
- References are declared per head, because the address a head can use differs per head.

### How a head runs

| Head | Process | Environment delivery |
|---|---|---|
| Desktop | `dotnet run --project <path> -f <tfm> --no-launch-profile` in the project folder | Process environment |
| WebAssembly | the same, plus an HTTP endpoint for the served app | Baked into the bootstrapper at build time |
| Android emulator | the same, plus `-p:AdbTarget=-e` | Baked into the APK at build time |

- **Target framework.** Defaults to the AppHost's .NET version with the head's suffix
  (`net10.0-desktop`, `net10.0-browserwasm`, `net10.0-android`). `WithTargetFramework` overrides
  it. The project is not evaluated to find it.
- **Launch profile.** None by default. Without `--no-launch-profile`, `dotnet run` takes the first
  profile, which may belong to another head. `WithLaunchProfile` opts in; a profile's own
  `environmentVariables` then override what Aspire injects.
- **Start.** The desktop head starts with the AppHost. The WebAssembly and Android heads do not:
  heads share one project and its `obj` folder, so starting several at once runs concurrent
  builds of it. This lowers the chance, it does not prevent it: a head started from the dashboard
  while another builds still races. Aspire's own `WithExplicitStart()` turns automatic start off
  for the desktop head.
- **Stop.** Stopping a head ends the app, not only `dotnet run`, whether the head or the whole
  AppHost is stopped. For the Android head, the app on the emulator ends too. No code of the integration does this: it is Aspire's process handling,
  verified on Windows and *unproven* on macOS and Linux. A head has no health check.
- **Publish.** In publish mode the project file is not checked, so a publish runs where the app
  is not checked out.
- **Build.** The head's environment reaches the build inside `dotnet run`, where MSBuild reads
  environment variables as properties and records them in a binary log.
- **Not published.** A head is left out of the deployment manifest.
- **Failure.** A failed build leaves the head failed, with the build output in its console log.

### Hot Reload

A head started by the AppHost with nothing else running logs "DevServer endpoint has no port: the
IDE did not provide a usable endpoint" and never connects. On the sample's desktop head, a XAML
edit reached the running window once two things were in place:

- **A dev server for the solution.** An IDE with the Uno extension starts one, or
  `dotnet dnx -y uno.devserver start` from the solution folder does. Either writes the port to the
  app's `.csproj.user`, which the head's build picks up.
- **`DOTNET_MODIFIABLE_ASSEMBLIES=debug` on the head.** `dotnet run --no-launch-profile` does not
  set it.

The two were applied together; which of them a XAML-only edit needs was not isolated.

The integration sets the variable on every head and does nothing else: a solution is expected to
be open in an IDE, which provides the dev server. The how-to names the command above for a run from
a terminal.

### Desktop

Receives its environment like any process. Gets `WithOtlpExporter()` so the dashboard endpoint and
service name are injected.

With no dev server running, the Hot Reload client's failed connection attempts appear in the
dashboard as failed requests of the head. They stop once an IDE has the solution open.

### Baking the environment (WebAssembly and Android)

Proven for WebAssembly in lot 3 and for the Android emulator in lot 4.

When a head starts, the integration resolves its environment and writes a **values file** into
the app's own `obj` folder, `obj/uno-aspire/<target framework>.v1.values.proj`. It is a small
project that returns name and value pairs, the same for every platform.

The client package `Uno.Extensions.Aspire` ships build logic (`buildTransitive`) that calls it
when it exists and turns the pairs into what the platform understands: `WasmShellMonoEnvironment`
items for WebAssembly; for Android, the `RuntimeEnvironmentVariable` items `dotnet run -e` adds,
added before .NET for Android's `_GenerateEnvironmentFiles`, which writes them into the app's own
environment file. Nothing is passed on the command
line, so every build of the app finds it: the head's own, and the one an IDE makes while the head
runs.

What depends on the head kind is decided by the AppHost, which knows it: the allow-list below,
and for Android the names in upper case, the loopback rewrite and leaving out `https`. The build
logic only maps.

The file is valid while the AppHost runs:

- It is written at every start of the head, never before and never in publish mode. Whether a
  value changed in the dashboard during a run reaches the next start is not measured.
- Stopping the head alone leaves it: the services it points to still run.
- The AppHost removes it when it stops, and again when it starts, for a file left by an AppHost
  that was killed. Until then, such a file puts a dead address into Debug builds. The build output
  lists the names it wrote and the file to delete when no AppHost runs.
- A removal that fails is a warning in the AppHost's own log, which outlives the dashboard.

Consequences:

- The app's csproj names no resource, but the app has to reference `Uno.Extensions.Aspire`.
  Without it the head runs and the app has no address. When the head starts, the AppHost reads the
  app's `obj/project.assets.json` and warns if the package is not among its libraries. Before the
  app's first restore there is no such file, and nothing is checked.
- The path is the app's project folder, so the projects the app references, which have their own
  folder, read nothing.
- The AppHost and the client package agree on the path and the format of the file. The format
  version is in the file name: a client that finds only another version's file warns, naming both
  packages, and reads nothing. They ship at the same version.
- Two heads of the same app for the same target framework share one file.
- Two AppHosts running the same app remove each other's file. A head whose target framework
  changed after an AppHost was killed leaves the old framework's file until it is deleted.

**What may be baked is an allow-list.** A baked value is readable by anyone who loads the page or
unpacks the APK.

- Baked: `services__*` variables whose value is an endpoint of a resource in the model.
- Everything else, including connection strings, parameters, literal values and `OTEL_*`, is left
  out. It does not stop the head: Aspire itself adds a variable such as `API_HTTP` to every
  reference, so refusing unknown names would stop every head.
- The AppHost opts a single variable in by name with `WithBakedEnvironment`, whatever its value.
- The check follows a value into the expressions it is built from, so an endpoint wrapped around a
  secret parameter is left out.

The head's log says what happened, with names and never values:

- Information: the names written into the app.
- Information: for Android, the service addresses left out over `https` on the host's loopback.
- Warning: a `services__*` variable left out, because the app will not reach that service.
- Warning: for Android, a service with no address left once those are out.
- Warning: a name opted in that the head does not have, or one whose value holds a secret.
- Debug: the other names left out.

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
- The head has an HTTP health check on `/`: it is unhealthy while it builds and healthy once the
  app is served.
- The served endpoint is local, not external.
- The dev server's own variables (`ASPNETCORE_URLS`, `DOTNET_MODIFIABLE_ASSEMBLIES`) stay in its
  process and are not baked.
- No `WithOtlpExporter()`: the dashboard key would be public in the page. See *Open questions*.
- The browser calls the API directly, so the API needs a CORS policy. The how-to restricts it to
  the WebAssembly head's origin, passed to the API by the AppHost, not to any origin.

### Android emulator

Implemented in lot 4. The sample first ran this head through a preview integration built for
another framework, with the address set by hand; a stable package cannot depend on that.

- The head runs `dotnet run -f net10.0-android -p:AdbTarget=-e`, which deploys to the running
  emulator and stays attached to the app.
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
- The build logic hooks a target and an item of .NET for Android that are not public, verified on
  SDK 36.1.118. After the build it checks that the values reached the environment file, and warns
  naming them when they did not, for an older SDK or a renamed target.
- An API that redirects `http` to `https` (`UseHttpsRedirection`) sends the emulator to the
  untrusted certificate. The how-to says so.
- No `WithOtlpExporter()`: see *Open questions*.
- The head needs exactly one running emulator. Booting one is out of scope; with several
  emulators, or a device attached as well, `-p:AdbTarget=-e` is not enough to pick one and is not
  verified.

## Out of scope

- iOS, physical Android devices and WinAppSDK heads. A device needs the API behind an
  authenticated tunnel.
- Publishing or deploying an Uno head through Aspire. This is the local development loop only.
- Booting an emulator from the dashboard.
- A server-side service defaults project: the Aspire template covers it.
- Detecting the app's target frameworks by evaluating the project.

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
   header redaction in `DiagnosticHandler`; the size measurement; a how-to under `doc/Learn/` that
   uses `AddExecutable` for a desktop head.
2. **AppHost package, desktop.** `AddUnoApp`, `AddDesktop`, `WithTargetFramework`,
   `WithLaunchProfile`, the start and stop behavior.
3. **AppHost package, WebAssembly.** Opens with the baking spike. `AddWebAssembly`, the values
   file and the client package's build logic that reads it, the allow-list.
4. **AppHost package, Android emulator.** `AddAndroidEmulator`, the `http`-only rewrite.
5. **Feature and template.** An `Aspire` entry in `UnoFeatures` and a template option. Both live
   in other repositories and follow the packages.
6. **Telemetry on WebAssembly and Android**, if questions 1 and 2 resolve to doing it.

Lots 1 and 2 are enough to run an app's desktop head with its backend in one step.

## Tests

`Uno.Extensions.Aspire.Tests`:

- With no OTLP endpoint in configuration, no OpenTelemetry service is registered.
- With one, a span from an `HttpClient` call reaches an in-memory exporter.
- A client with `BaseAddress` `https+http://api` sends its request to the address in
  `services:api:http:0`.
- A literal `Url` is used as is, even when `services:api:http:0` is set.
- Against a stub handler that always returns 503, with retry delays set to zero: a `GET` is
  attempted four times, a `POST` once.
- The same stub behind a Kiota client: four attempts with `UseServiceDefaults`, in either call
  order with `UseHttp`; Kiota's own count without it; the app's count when the app registers a
  `RetryHandlerOption`.
- A client that calls `RemoveAllResilienceHandlers()` makes one attempt.
- A container-registered delegating handler runs once per attempt.
- `DiagnosticHandler` does not log a query string or the value of a credential header.

`Uno.Extensions.Aspire.Hosting.Tests`, on the application model with endpoints allocated by the
test, without starting a process:

- Each head's command line carries its `-f`, `--no-launch-profile` by default, and the profile
  name after `WithLaunchProfile`.
- A referenced endpoint appears in the desktop head's environment, and in the values file for
  WebAssembly and Android.
- A connection string, a secret parameter, a literal value and an endpoint wrapped around a secret
  are each left out for a baked head; a variable opted in by name is baked.
- A value containing MSBuild or XML special characters round-trips; one with a line break is refused.
- Android names are written in upper case; a loopback service address is rewritten over `http`, left out
  over `https` (with a warning when the service has no address left), and kept when it is not on
  the loopback; an opted-in `services__*` variable is rewritten like the others and any other
  opted-in value is not; an encoded path stays encoded; two names differing only by case stop the
  head.
- Writing the same environment twice leaves the file untouched; a changed value rewrites it.
- The AppHost's start removes a file and temporary files left by an earlier run, and its stop
  removes the one it wrote. With nothing written, removing logs nothing.
- The log warns for a left-out service address, a missing opt-in and a secret opt-in, and never
  carries a value.
- A restored app without `Uno.Extensions.Aspire` among its libraries gets a warning; one with a
  package whose name only starts the same, or with the name outside `libraries`, does not count.

Build tests (`Given_AppBuildLogic`) run the client package's build logic with MSBuild, in a
process, on a project that stands in for the app, against the file the AppHost wrote:

- A Debug build carries the values, including MSBuild and XML special characters, and its output lists
  the names, not the values. An Android build hands them to the environment file, carries none
  when optimized, and warns when the SDK does not write them.
- Another configuration, an optimized build, a publish and another target framework carry none,
  and so does a build with no file.
- A file of another format version gives a warning and no values.

One end-to-end sample under `samples/`, run by hand: AppHost, API, database and the three heads.
