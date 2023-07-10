; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/master/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|--------------------
KE0001 | Usage | Error   | A record eligible to IKeyEquatable generation must be partial.
KE0002 | Usage | Error   | A record that implements GetKeyHashCode should also implement KeyEquals.
KE0003 | Usage | Error   | A record that implements KeyEquals should also implement GetKeyHashCode.
KE0004 | Usage | Warning | A record flagged with [ImplicitKeyEquality] attribute must have an eligible key property.
KE0005 | Usage | Warning | A record should have only one matching key property for implicit IKeyEquatable generation.
KE0006 | Usage | Warning | A record that implements IKeyEquatable should also implement IKeyed.
PS0001 | Usage | Error | A property selector can only use property members.
PS0002 | Usage | Error | A property selector cannot have any closure.
PS0003 | Usage | Error | A property selector must be a lambda.
PS0004 | Usage | Error | The type of the entity of a PropertySelector must be a record.
PS0005 | Usage | Error | All types involved in a PropertySelector must be records.
PS0006 | Usage | Error | All types involved in a PropertySelector must be constructable without parameter.
PS0007 | Usage | Error | PS0007, [Documentation](https://aka.platform.uno/PS0007)
PS0101 | Usage | Warning | A method which accepts a PropertySelector must also have 2 parameters flagged with [CallerFilePath] and [CallerLineNumber].
PS0102 | Usage | Error | [CallerFilePath] and [CallerLineNumber] arguments used among a PropertySelector argument must be constant values.
PS9999 | Usage | Error | Code generation of PropertySelector failed for an unknown reason (see logs for more details).
