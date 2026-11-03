; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/master/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|--------------------
FEED1001 | Usage    | Info     | A member of a model or record hides the HasErrors property (validation state) of the generated bindable.
FEED2001 | Usage    | Error    | Unable to resolve the feed that is configured to be used as command parameter.
FEED2002 | Usage    | Error    | The property configured to be used as command parameter is not a Feed of the right type.
FEED2003 | Usage    | Warning  | The parameter of a command validated on execution is not a State, so the validation results cannot be published.
