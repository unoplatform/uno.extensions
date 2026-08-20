# macOS agent handoff — spec 010 item 8: live testbed validation

Audience: an agent running on the macOS machine with sibling checkouts of **uno.extensions** and
**Uno.Samples**. Goal: prove, on a real Skia iOS head, that the spec-010 fix works end to end —
the Uno.Sdk swap still fires, but the swapped-in plain `net9.0` lib is now functional and the MSAL
provider registers and derives the iOS redirect URI at runtime.

Read `spec.md` in this folder first for the mechanism. This file is only the run book.

## Prerequisites

1. **uno.extensions** @ `dev/sb/msal-auth-fixes`, pulled to a commit that contains the spec-010
   implementation (the commit whose message references spec 010 / runtime dispatch — if the newest
   commit is still `feat: add spec for Skia-mobile runtime dispatch...` you only have the spec,
   not the fix; stop and ask for a push). Quick sanity check that you have the fix:
   `grep -n "OperatingSystem.IsAndroid()" src/Uno.Extensions.Authentication.MSAL/MsalAuthenticationProvider.cs`
   must hit inside `CurrentRedirectPlatform`.
2. **Uno.Samples** @ `dev/sb/msa-ext`. The testbed is `UI/Authentication.MsalExtensionsDemo`.
   Its `HANDOFF-MACOS.md` is the authority for sample-side wiring (local feed configuration,
   Entra registration values, prior iteration notes) — read it before building.
3. Xcode + iOS simulator; .NET SDK 10.x (needed both for the build and for the file-based probe
   script below).
4. Optional: an Android emulator for the repeat pass.

## The iteration loop (extensions → sample)

From the uno.extensions repo root:

```bash
# 1. Pack the provider package at the local version the demo consumes
dotnet build src/Uno.Extensions.Authentication.MSAL/Uno.Extensions.Authentication.MSAL.WinUI.csproj \
  -c Release /p:PackageVersion=255.255.255.255-local \
  /p:PackageOutputPath=<local-feed-path-from-HANDOFF-MACOS>
```

(On macOS `Build_Windows` defaults to false, so the package has no `windows10` lib — known,
harmless for this validation; see the spec's caveats.)

```bash
# 2. Purge the cached copy — NuGet will not refresh a same-version package
rm -rf ~/.nuget/packages/uno.extensions.authentication.msal.winui/255.255.255.255-local

# 3. In the demo folder: delete the ios bin/obj trees.
#    NOT optional — incremental build does not re-run the swap, so a stale swapped dll survives.
cd <Uno.Samples>/UI/Authentication.MsalExtensionsDemo
rm -rf **/bin/**/net10.0-ios* **/obj/**/net10.0-ios*   # or just delete bin/ and obj/ wholesale

# 4. Rebuild the ios head with a binlog
dotnet build -f net10.0-ios -bl:msal-ios.binlog
```

## Verification gates (all four, in order)

**Gate 1 — the swap fired (expected, by design).** The binlog must still contain
`Replacing uno.extensions.authentication.msal.winui`. Success is the swapped lib being
functional, *not* the swap disappearing. If the string is absent, you are not testing the
spec-010 code path — check the package/feed wiring.

```bash
# crude but effective; use the msbuild structured log viewer if available
grep -c "Replacing uno.extensions.authentication.msal.winui" <(strings msal-ios.binlog) || true
```

**Gate 2 — IL probe on the dll inside the built .app.** Find
`Uno.Extensions.Authentication.MSAL.WinUI.dll` under the `.app` bundle in `bin/` and run the
probe. Expected: **~113 bytes** (functional). **2 bytes = the stub regression is back; 11 bytes =
you probed a maccatalyst build by mistake.**

Save as `probe.cs`, run with `dotnet run probe.cs -- <path-to-dll>`:

```csharp
// IL-body-size probe from specs/010: 2 bytes = stub (ldarg.0; ret), ~113 = functional.
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

var path = args[0];
using var stream = File.OpenRead(path);
using var pe = new PEReader(stream);
var md = pe.GetMetadataReader();

foreach (var tdHandle in md.TypeDefinitions)
{
	var td = md.GetTypeDefinition(tdHandle);
	if (md.GetString(td.Name) != "HostBuilderExtensions")
	{
		continue;
	}

	foreach (var mHandle in td.GetMethods())
	{
		var m = md.GetMethodDefinition(mHandle);
		if (md.GetString(m.Name) != "InternalAddMsal")
		{
			continue;
		}

		var body = pe.GetMethodBody(m.RelativeVirtualAddress);
		Console.WriteLine($"{path}: InternalAddMsal IL body = {body.GetILBytes()!.Length} bytes");
		return;
	}
}

Console.WriteLine($"{path}: InternalAddMsal not found");
```

**Gate 3 — runtime, unattended (deploy to the simulator and read startup logs).**

- MUST appear (Information):
  `Using RedirectUri 'msauth.{bundleId}://auth'; sign-in requires a matching redirect URI on the app registration`
  — the bundle id comes from the WinRT layer at runtime; this line existing at all proves the
  provider registered *and* runtime dispatch picked iOS.
- MUST NOT appear: `No providers specified` (the spec-010 symptom).
- If Trace logging is on: `RedirectUri resolution: PlatformDerived`.

**Gate 4 — runtime, needs a human at the Entra prompt (interactive).** Sign in on LoginPage,
then Logout on MainPage → must navigate back to LoginPage with the back stack cleared (already
implemented demo-side in `MainModel.Logout`). If no human is available, report gates 1–3 and
mark gate 4 as not run — gates 1–3 alone already refute/confirm the regression fix.

## Optional: Android repeat

Same loop with `-f net10.0-android` and an emulator. Expected redirect line uses the Android
convention: `msal{ClientId}://auth`. Same four gates.

## On success

1. In this folder's `spec.md`: tick plan item 8 and append the results (gate outcomes, byte
   count, the redirect log line) to the "Verification" section.
2. If anything failed, do NOT tick item 8 — record what failed in `spec.md` with the binlog +
   probe output and stop; the failure analysis belongs back in the extensions repo.
3. Update `UI/Authentication.MsalExtensionsDemo/HANDOFF-MACOS.md` (samples repo) with anything
   you learned about the iteration loop that it does not already say.
