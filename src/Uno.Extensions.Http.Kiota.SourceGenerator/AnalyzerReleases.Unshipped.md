; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/master/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|--------------------
KIOTA001 | Kiota.SourceGenerator | Error | Failed to parse the OpenAPI document
KIOTA002 | Kiota.SourceGenerator | Error | Unsupported OpenAPI version
KIOTA003 | Kiota.SourceGenerator | Error | Missing required Kiota configuration (e.g. KiotaClientName)
KIOTA010 | Kiota.SourceGenerator | Warning | Non-fatal OpenAPI parsing warning
KIOTA020 | Kiota.SourceGenerator | Warning | OpenAPI spec exceeds recommended size for source generation
KIOTA030 | Kiota.SourceGenerator | Info | Kiota code generation completed successfully
KIOTA031 | Kiota.SourceGenerator | Warning | Kiota code generation completed with some types skipped
KIOTA040 | Kiota.SourceGenerator | Error | Unexpected error in the Kiota source generator pipeline
KIOTA050 | Kiota.SourceGenerator | Warning | Failed to generate source for an individual type
KIOTA051 | Kiota.SourceGenerator | Error | Failed to build code model (CodeDOM) from OpenAPI document
