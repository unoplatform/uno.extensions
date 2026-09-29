; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/master/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|--------------------
MOCK0001 | Usage    | Warning  | No mock is generated for a model whose view-model exposes no public constructor.
MOCK0002 | Usage    | Info     | No mock is generated for a model none of whose feeds is fed by a constructor parameter.
MOCK0003 | Usage    | Warning  | An implicit model pattern that is not a valid regular expression.
