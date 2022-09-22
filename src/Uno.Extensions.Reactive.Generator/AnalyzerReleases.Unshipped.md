; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/master/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|--------------------
FEED2001 | Usage    | Error    | Unable to resolve the feed that is configured to be used as command parameter.
FEED2002 | Usage    | Error    | The property configured to be used as command parameter is not a Feed of the right type.
KE0001 | Usage | Error   | A record eligible to IKeyEquatable generation must be partial.
KE0002 | Usage | Error   | A record that implements GetKeyHashCode should also implement KeyEquals.
KE0003 | Usage | Error   | A record that implements KeyEquals should also implement GetKeyHashCode.
KE0004 | Usage | Warning | A record flagged with [ImplicitKeyEquality] attribute must have an eligible key property.
KE0005 | Usage | Warning | A record should have only one matching key property for implicit IKeyEquatable generation.
