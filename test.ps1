$ErrorActionPreference = 'Stop'

# 1. Build
dotnet msbuild src/Uno.Extensions.RuntimeTests/Uno.Extensions.RuntimeTests/Uno.Extensions.RuntimeTests.csproj `
    -p:Configuration=Debug `
    -p:UnoTargetFrameworkOverride=net9.0-desktop `
    -restore -v:minimal

# 2. Env vars (some optional)
$env:UNO_RUNTIME_TESTS_RUN_TESTS = '{"Filter":{"Value":"When_MainPageGetsTabBarPagesAndRoutesAddedViaHR_Then_FirstPageIsDefaultlySelected"}}'
$env:UNO_RUNTIME_TESTS_OUTPUT_PATH = './test-results'
$env:DOTNET_MODIFIABLE_ASSEMBLIES = 'debug'

# 3. cd to build output location (important when running HR tests - the secondary app spawns here)
Push-Location src/Uno.Extensions.RuntimeTests/Uno.Extensions.RuntimeTests/bin/Uno.Extensions.RuntimeTests/Debug/net9.0-desktop
try {
    # 4. Run
    dotnet Uno.Extensions.RuntimeTests.dll
}
finally {
    Pop-Location
}
