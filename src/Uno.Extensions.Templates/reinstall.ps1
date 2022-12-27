param(
    # Version of published Uno.Extensions packages
    [string]$Version = "255.255.255.255"
)
dotnet new uninstall Uno.Extensions.Templates
dotnet pack -p:Version=$Version
dotnet new install $PSScriptRoot\bin\Uno.Extensions.Templates\Debug\Uno.Extensions.Templates.$Version.nupkg
