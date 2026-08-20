; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/master/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|--------------------
FEED2001 | Usage    | Error    | Unable to resolve the feed that is configured to be used as command parameter.
FEED2002 | Usage    | Error    | The property configured to be used as command parameter is not a Feed of the right type.
FEED3001 | Usage    | Warning  | Mock generation is enabled for a model whose base model is not mock-enabled; no mock factory is generated.
FEED3002 | Usage    | Warning  | A GenerateModelMocks pattern does not match any generated bindable view model of the assembly.
