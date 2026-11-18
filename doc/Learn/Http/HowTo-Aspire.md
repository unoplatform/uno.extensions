---
uid: Uno.Extensions.Http.HowToAspire
---
# How-To: Run an app with its backend under Aspire

[Aspire](https://aspire.dev) starts every part of a solution with one command and tells each part where the others are. This guide adds an Uno Platform desktop app to an Aspire AppHost, next to the API it calls.

The `Uno.Extensions.Aspire` package gives the app the client half of Aspire's service defaults:

* **Service discovery**: the app calls the API by a logical name, and Aspire supplies the real address.
* **Resilience**: timeouts, retries and a circuit breaker on every `HttpClient`.
* **Telemetry**: the app's HTTP calls and logs appear in the Aspire dashboard.

## Step-by-Step Guide

> [!IMPORTANT]
> This guide assumes you used the template wizard or `dotnet new unoapp` to create your solution, with the `Http` or `HttpKiota` UnoFeature, and that the solution already has an Aspire AppHost project that starts your API.

The steps cover the **desktop** target. For the browser and the Android emulator, see [WebAssembly](#webassembly) and [Android emulator](#android-emulator).

### 1. Installation

* Add the package to the app project:

    ```xml
    <ItemGroup>
        <PackageReference Include="Uno.Extensions.Aspire" />
    </ItemGroup>
    ```

    Add a `Version` if the solution does not use central package management. Use the same version as the other `Uno.Extensions` packages: an older `Uno.Extensions.Http.Kiota` does not turn its own retries off, see [Kiota clients](#kiota-clients).

### 2. Enable the service defaults

* Call `UseServiceDefaults()` on the host builder in `App.xaml.cs`:

    ```csharp
    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        var appBuilder = this.CreateBuilder(args)
            .Configure(host => host
                .UseConfiguration(configure: config => config.EmbeddedSource<App>())
                .UseServiceDefaults()
                .UseHttp((context, services) => services
                    .AddKiotaClient<MyApiClient>(context, name: "MyApi")));
        ...
    }
    ```

    The order of `UseServiceDefaults()` and `UseHttp()` does not matter.

### 3. Point the endpoint at the logical name

* In `appsettings.json`, use the name the AppHost gives the API, here `api`:

    ```json
    {
      "MyApi": {
        "Url": "https+http://api"
      }
    }
    ```

    `https+http` means "use https when the API exposes it, otherwise http".

> [!IMPORTANT]
> Use the logical form in **every** settings file the desktop app loads under the AppHost, including `appsettings.development.json`. A real address in any of them is used as is: the address from Aspire is ignored, and nothing reports it.
>
> For a published app, set a real `https://` address in the production settings file. A logical name that nothing resolves is called as is, and the request fails with a name resolution error for the host `api`.

### 4. Add the app to the AppHost

Aspire's `AddProject` does not accept a project that targets several frameworks. The `Uno.Extensions.Aspire.Hosting` package adds the app by its path and runs one target of it.

* Add the package to the **AppHost** project, not to the app:

    ```xml
    <ItemGroup>
        <PackageReference Include="Uno.Extensions.Aspire.Hosting" />
    </ItemGroup>
    ```

    The API is experimental. Add `UNOASPIRE001` to the AppHost's `NoWarn` to use it.

* In the AppHost's `Program.cs`:

    ```csharp
    var api = builder.AddProject<Projects.MyApp_Api>("api");

    builder.AddUnoApp("app", "../MyApp/MyApp.csproj")
        .AddDesktop()
        .WithReference(api)
        .WaitFor(api);
    ```

  * `AddUnoApp` takes the path of the app's project, relative to the AppHost folder.
  * `AddDesktop()` adds the resource `app-desktop`, which runs the project's desktop target, for example `net10.0-desktop`, and sends its telemetry to the dashboard.
  * `WithReference(api)` passes the API's address to the app.

* The target framework comes from the project file. When the project computes its target frameworks from properties, the AppHost's .NET version is used. To choose another one or a launch profile, add `.WithTargetFramework("net9.0-desktop")` or `.WithLaunchProfile("MyApp (Desktop)")`. Launch profiles are for the desktop head only. Environment variables of a launch profile override the ones Aspire passes, the API's address and the telemetry endpoint included.

* Do not add a `ProjectReference` from the AppHost to the app.

### 5. Run it

* Run the AppHost:

    ```bash
    dotnet run --project MyApp.AppHost
    ```

    The dashboard opens, the API starts, and the app starts once the API is ready. Each head is built before it starts: it shows as **Building**, or as **Queued** while another head of the app builds, since heads share the project's `obj` folder. Its build output is in its console log.

* Hot Reload needs a dev server. An IDE that has the solution open provides one. From a terminal, start one in the solution folder before the AppHost:

    ```bash
    dotnet dnx -y uno.devserver start
    ```

### If the app does not start

* A build error stops the head. The build output is in its console log in the dashboard.
* A message that the project does not target the head's framework names the ones it does. Set one with `WithTargetFramework`.
* Failed requests of `app-desktop` to a `localhost` port that is not the API are Hot Reload looking for a dev server. Start one as above, or ignore them.
* The values Aspire passes are also visible to the app's build. Do not share a binary log of a build started by the AppHost.

## WebAssembly

A browser has no process environment, so the address of the API is written into the app when it is built. The `Uno.Extensions.Aspire` package from step 1 does this; the app's project needs no other change.

* Add the head in the AppHost's `Program.cs`, and tell the API which origin the browser calls from:

    ```csharp
    var app = builder.AddUnoApp("app", "../MyApp/MyApp.csproj");

    app.AddDesktop().WithReference(api).WaitFor(api);

    var wasm = app.AddWebAssembly().WithReference(api).WaitFor(api);

    api.WithEnvironment("Cors__Origin", wasm.GetEndpoint("http"));
    ```

* In the API, allow that origin and no other:

    ```csharp
    if (app.Configuration["Cors:Origin"] is { } origin)
    {
        app.UseCors(policy => policy.WithOrigins(origin).AllowAnyHeader().AllowAnyMethod());
    }
    ```

    The API then allows no cross-origin call when it runs without the AppHost.

* Start `app-wasm` from the dashboard. It does not start with the AppHost, so a run does not build every head. Once it is healthy, open its URL.

What to know:

* Only the addresses of referenced services reach the app. Any other variable set on the head is left out, and the head's log names it. To pass one, add `.WithClientEnvironment("Name")`. Anyone who opens the page can read a value passed this way, so never pass a secret.
* If the API exposes `https`, the app uses it, and the browser has to trust the API's development certificate.
* Telemetry is not exported from the browser.
* The address is in every Debug build of the WebAssembly target made while the AppHost runs, including the ones your IDE makes. Release builds and publishes never carry it.
* The AppHost keeps the address in the app's `obj/uno-aspire` folder and removes it when it stops. A build that uses it lists the names in its output.

### If the WebAssembly app cannot reach the API

* **CORS error in the browser console:** the API does not allow the head's origin. Check that the AppHost passes `Cors__Origin` and that the API reads it.
* **Certificate error:** the browser does not trust the API's development certificate. Run `dotnet dev-certs https --trust`.
* **The app calls `api` by name, or calls nothing:** the address is not in the app. Read the head's log: a warning says when the app does not reference `Uno.Extensions.Aspire`, or names a service address that was left out.
* **The app calls an address nothing listens on:** an AppHost that was killed instead of stopped leaves its address in `obj/uno-aspire`. The next AppHost start removes it; until then, delete that folder.
* **Build warning `UNOASPIRE101`, asking for the same version of both packages:** the AppHost's `Uno.Extensions.Aspire.Hosting` and the app's `Uno.Extensions.Aspire` differ. Update them to the same version.
* **Publishing:** do not publish with `--no-build` after a build started by the AppHost. That copies the development address into the output.

## Android emulator

An Android app does not inherit the environment of the command that deploys it, so the address of the API is written into the app when it is built, as for WebAssembly. The `Uno.Extensions.Aspire` package from step 1 does this.

* Start an Android emulator. The AppHost deploys to the one running; it does not boot one.
* Add the head in the AppHost's `Program.cs`:

    ```csharp
    app.AddAndroidEmulator().WithReference(api).WaitFor(api);
    ```

* Allow plain `http` to `10.0.2.2`, the emulator's address for your machine, and to nothing else. Add `Platforms/Android/Resources/xml/network_security_config.xml`:

    ```xml
    <?xml version="1.0" encoding="utf-8"?>
    <network-security-config>
      <domain-config cleartextTrafficPermitted="true">
        <domain includeSubdomains="false">10.0.2.2</domain>
      </domain-config>
    </network-security-config>
    ```

    and reference it in `Platforms/Android/AndroidManifest.xml`:

    ```xml
    <application android:networkSecurityConfig="@xml/network_security_config" />
    ```

* Start `app-android` from the dashboard. The first start builds and deploys the app, which takes a few minutes. Stopping the head closes the app on the emulator.

What to know:

* A service address on your machine reaches the app with `localhost` replaced by `10.0.2.2`, over `http` only. Its `https` address is left out, because the emulator does not trust your machine's development certificate, so `https+http://api` resolves to `http`.
* The variable names are written in upper case, because Android drops lowercase ones. Configuration keys ignore case, so the app reads them as usual.
* `.WithClientEnvironment("Name")` passes one more variable, with its name in upper case; a `services__` variable is treated as a service address. Anyone who unpacks the APK can read it, so never pass a secret.
* The network security configuration is in every build. On a device, `10.0.2.2` can be another machine on the network; a Release build is never configured to call it.
* Telemetry is not exported from the emulator.
* As for WebAssembly, the address is in every Debug build made while the AppHost runs, and in no Release build or publish.
* It needs the Android workload 36.1.118 or later, which writes these values into the app; an older one gives build warning `UNOASPIRE102`.

### If the Android app cannot reach the API

* **The head stops right away:** check that exactly one emulator runs: `adb devices`.
* **The head's log warns that the app gets no address for a service:** the API offers only `https` on your machine. Give it an `http` endpoint.
* **The app's log says cleartext traffic is not permitted:** the network security configuration is missing or not referenced in the manifest.
* **The API answers with a redirect to `https`:** it calls `UseHttpsRedirection()`. Skip that call in Development, or the emulator ends on the untrusted certificate.
* **Build warning `UNOASPIRE102`, saying the SDK did not write the environment:** update the Android workload, `dotnet workload update`.
* **The build output has no `Uno.Extensions.Aspire: written into the app` line:** the app does not reference `Uno.Extensions.Aspire`; the head's log warns about it too.

## Debugging the app

The heads are processes the AppHost starts; your IDE does not attach to them. To debug the app, keep the AppHost running for the backend and start the app from your IDE:

* **Desktop:** the app reads the address from the environment or from a `services` section, see below.
* **WebAssembly and Android:** a Debug build reads the address the AppHost wrote, once the head has been started from the dashboard in this AppHost run. Stop the head, then start the app from your IDE.

## Running the app without the AppHost

Nothing supplies the address, so give it yourself, in a `services` section of a settings file:

```json
{
  "services": {
    "api": {
      "http": [ "http://localhost:5171" ]
    }
  }
}
```

or in the environment before starting the app:

```bash
# bash
export services__api__http__0=http://localhost:5171
```

```powershell
# PowerShell
$env:services__api__http__0 = "http://localhost:5171"
```

Under the AppHost, an address Aspire passes wins over the same key in a settings file, so the section can stay.

## Limits

* Supported heads: desktop, WebAssembly and the Android emulator. iOS, Mac Catalyst, Windows (WinAppSDK) and physical devices are not supported yet.
* The AppHost API is experimental (`UNOASPIRE001`).

## Resilience

`UseServiceDefaults()` adds the [standard resilience handler](https://learn.microsoft.com/dotnet/core/resilience/http-resilience) to every `HttpClient` the app creates through `IHttpClientFactory`, including clients that call other services.

| Default | Value |
|---|---|
| Timeout for one attempt | 10 seconds |
| Timeout for the whole request | 30 seconds |
| Retries | 3, for `GET` and other safe methods only |
| Circuit breaker | Opens when 10% of at least 100 calls in 30 seconds fail, for 5 seconds |

`POST`, `PUT`, `PATCH` and `DELETE` are never retried: a retried `POST` could create a duplicate.

A call that times out throws `Polly.Timeout.TimeoutRejectedException`, and a call made while the circuit is open throws `Polly.CircuitBreaker.BrokenCircuitException`. Neither is an `HttpRequestException`, so check your `catch` blocks.

To change a value for every client, or to turn the resilience handler or the telemetry off, pass options:

```csharp
.UseServiceDefaults(options =>
{
    options.ConfigureResilience = resilience => resilience.AttemptTimeout.Timeout = TimeSpan.FromSeconds(20);
    // options.UseResilience = false;
    // options.UseTelemetry = false;
})
```

With the resilience handler off, Kiota clients keep their own retries.

### Opt a client out

The timeouts run until the response headers arrive, so a large upload or a slow endpoint will exceed them. Remove the handler for that client:

```csharp
.UseHttp((context, services) => services
    .AddKiotaClient<UploadClient>(
        context,
        name: "Uploads",
#pragma warning disable EXTEXP0001 // RemoveAllResilienceHandlers is still marked experimental
        configure: (builder, _) => builder.RemoveAllResilienceHandlers()))
#pragma warning restore EXTEXP0001
```

### Kiota clients

Kiota has its own retry handler. With `UseServiceDefaults()`, `AddKiotaClient` turns it off so the resilience handler is the only one that retries.

* To keep Kiota's retries, register your own `RetryHandlerOption` in the container. Yours is used. Kiota also retries `POST`, so both layers then retry.
* A Kiota client that removes the resilience handler has no retries, unless you register a `RetryHandlerOption`.

## Telemetry

Telemetry is exported only when the app is given an OTLP endpoint (`OTEL_EXPORTER_OTLP_ENDPOINT`), which `AddDesktop()` does. An app started any other way registers no exporter and no instrumentation.

> [!WARNING]
> The check is the same in a Release build. A published app exports its logs, traces and metrics to whatever endpoint that variable or a settings file names. Do not set it in a settings file you ship.

* Traces and metrics cover `HttpClient` calls; logs come from `ILogger`.
* Telemetry is not exported on WebAssembly or from the Android emulator.
* `UseServiceDefaults()` owns the OTLP exporter. Do not register a second one.
* Batches are sent every few seconds. The last one is sent only if the app stops its host, so closing the window usually loses it.

## See also

* [How-To: Create a Kiota Client](xref:Uno.Extensions.Http.HowToKiota)
* [How-To: Configure with endpoint options](xref:Uno.Extensions.Http.HowToEndpointOptions)
