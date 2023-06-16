#!/bin/bash
set -euo pipefail
IFS=$'\n\t'

export UNO_UITEST_TARGETURI=http://localhost:5000
export UNO_UITEST_DRIVERPATH_CHROME=$BUILD_SOURCESDIRECTORY/build/node_modules/chromedriver/lib/chromedriver
export UNO_UITEST_CHROME_BINARY_PATH=$BUILD_SOURCESDIRECTORY/build/node_modules/puppeteer/.local-chromium/linux-991974/chrome-linux/chrome
export UNO_UITEST_SCREENSHOT_PATH=$BUILD_ARTIFACTSTAGINGDIRECTORY/screenshots/wasm
export BIN_LOG_PATH=$BUILD_ARTIFACTSTAGINGDIRECTORY/wasm-uitest.binlog
export UNO_UITEST_PLATFORM=Browser
export UNO_UITEST_CHROME_CONTAINER_MODE=true
export UNO_UITEST_PROJECT=$BUILD_SOURCESDIRECTORY/testing/TestHarness/TestHarness.UITest/TestHarness.UITest.csproj
export UNO_UITEST_BINARY=$BUILD_SOURCESDIRECTORY/testing/TestHarness/TestHarness.UITest/bin/Release/net48/TestHarness.UITest.dll
export UNO_UITEST_LOGFILE=$BUILD_ARTIFACTSTAGINGDIRECTORY/screenshots/wasm/nunit-log.txt
export UNO_UITEST_WASM_PROJECT=$BUILD_SOURCESDIRECTORY/testing/TestHarness/TestHarness.Wasm/TestHarness.Wasm.csproj
export UNO_UITEST_WASM_OUTPUT_PATH=$BUILD_SOURCESDIRECTORY/testing/TestHarness/TestHarness.Wasm/bin/Release/net7.0/dist/
export UNO_UITEST_NUNIT_VERSION=3.13.2
export UNO_UITEST_NUGET_URL=https://dist.nuget.org/win-x86-commandline/v5.7.0/nuget.exe
export UNO_UITEST_WASM_PROJECT_BUILD_OPTIONS="/p:UnoExtensionsDisableNet7=true /p:GeneratePackageOnBuild=false"

cd $BUILD_SOURCESDIRECTORY

dotnet build -c Release $UNO_UITEST_PROJECT
dotnet build -c Release $UNO_UITEST_WASM_PROJECT /p:IsUiAutomationMappingEnabled=True /p:UseWebAssemblyAOT=false /bl:$BIN_LOG_PATH $UNO_UITEST_WASM_PROJECT_BUILD_OPTIONS

# Start the server
dotnet run --project $UNO_UITEST_WASM_PROJECT -c Release --no-build &

cd $BUILD_SOURCESDIRECTORY/build

npm i chromedriver@102.0.0
npm i puppeteer@14.1.0
wget $UNO_UITEST_NUGET_URL
mono nuget.exe install NUnit.ConsoleRunner -Version $UNO_UITEST_NUNIT_VERSION

mkdir -p $UNO_UITEST_SCREENSHOT_PATH

mono $BUILD_SOURCESDIRECTORY/build/NUnit.ConsoleRunner.$UNO_UITEST_NUNIT_VERSION/tools/nunit3-console.exe \
  --trace=Verbose --inprocess --agents=1 --workers=1 \
  $UNO_UITEST_BINARY || true
