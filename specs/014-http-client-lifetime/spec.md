# 014 — Http: choose the typed client's service lifetime in AddClient

**Status: in progress.** Written 2026-08-22 on branch `dev/sb/auth-providers-fixes` (self-contained
commit, intended to be cherry-picked into its own PR - it touches only `Uno.Extensions.Http`).

## Problem

`AddClient`/`AddClientWithEndpoint` register typed clients through
`AddHttpClient<TClient>(name)`, which is always **transient**. A client that carries flow state
across resolutions (the motivating case: an OAuth helper whose PKCE verifier must survive from
the authorization request to the code exchange, resolved separately by each
`AddWeb` callback) has to either externalize its state into a separate singleton or hand-roll a
singleton registration against the named pipeline - both workarounds for a missing knob.

## Design

New public overloads (the existing methods are untouched: adding an optional parameter to a
shipped method is binary-breaking for a public NuGet API):

- `AddClient<TClient, TImplementation>(context, ServiceLifetime lifetime, ...)`
- `AddClient<TInterface>(context, ServiceLifetime lifetime, ...)`
- `AddClientWithEndpoint<TClient, TImplementation, TEndpoint>(context, ServiceLifetime lifetime, ...)`
- `AddClientWithEndpoint<TInterface, TEndpoint>(context, ServiceLifetime lifetime, ...)`

`lifetime` is a required parameter placed before the existing optional ones, so existing call
sites keep binding to the old overloads and nothing is ambiguous.

Behavior:

- `Transient`: delegates to the existing overloads - identical behavior, including
  `AddClient<TInterface>` accepting an interface (Refit and Kiota supply the implementation).
- `Singleton`/`Scoped`: the endpoint pipeline (base address, native handler, delegating handlers,
  `configure`) is registered as a **named** client, and the client type is registered with the
  requested lifetime as a factory that builds the instance through
  `ITypedHttpClientFactory<TImplementation>` over `IHttpClientFactory.CreateClient(name)` - the
  supported way to construct typed clients manually.
- The named client is `name` when one is supplied, else the client type's **full** name. A bare
  `AddHttpClient(name)` has none of `AddHttpClient<T>`'s duplicate-name checking, so short names
  would let two `Api` types in different namespaces silently share one pipeline - base address,
  delegating handlers and authorization included. The configuration section is resolved exactly
  as the transient overloads resolve it (`name`, else the short type name less a leading `I`);
  only the `HttpClient` name differs.
- `AddClient<TInterface>(..., Singleton|Scoped)` with an interface throws `ArgumentException` at
  registration: that shape registers the type as its own implementation, which for an interface
  would otherwise fail only at the first resolve.
- XML docs call out the trade-off: a non-transient client captures its `HttpClient`, forgoing the
  factory's handler rotation (stale-DNS mitigation), which is the reason typed clients default to
  transient.

## Tests

New `Uno.Extensions.Http.Tests` (plain net9.0, package CI's `**/*.Tests.dll` filter): transient
default unchanged, singleton returns the same instance with the endpoint's `Url` applied, scoped
varies across scopes, interface/implementation pair resolves through the interface, custom
`TEndpoint` binds from configuration, an explicit `Transient` matches the default, the no-name
path binds the type-named section, two same-named singletons keep separate pipelines, and an
interface is rejected for a non-transient lifetime but accepted for `Transient`.
