param(
    # Version of published Uno.Extensions packages
    [string]$Version = "255.255.255.255"
)
dotnet new uninstall Uno.Extensions.Templates
dotnet build -p:Version=$Version -c Release
if($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
dotnet new install $PSScriptRoot\bin\Uno.Extensions.Templates\Release\Uno.Extensions.Templates.$Version.nupkg
