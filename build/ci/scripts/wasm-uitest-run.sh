#!/bin/bash
set -euo pipefail
IFS=$'\n\t'

export UNO_UITEST_TARGETURI=http://localhost:5000
export UNO_UITEST_DRIVERPATH_CHROME=$BUILD_SOURCESDIRECTORY/build/node_modules/chromedriver/lib/chromedriver
export UNO_UITEST_CHROME_BINARY_PATH=~/.cache/puppeteer/chrome/linux-119.0.6045.105/chrome-linux64/chrome
export UNO_UITEST_SCREENSHOT_PATH=$BUILD_ARTIFACTSTAGINGDIRECTORY/screenshots/wasm
export BIN_LOG_PATH=$BUILD_ARTIFACTSTAGINGDIRECTORY/wasm-uitest.binlog
export UNO_UITEST_PLATFORM=Browser
export UNO_UITEST_CHROME_CONTAINER_MODE=true
export UNO_UITEST_PROJECT=$BUILD_SOURCESDIRECTORY/testing/TestHarness/TestHarness.UITest
export UNO_UITEST_LOGFILE=$BUILD_ARTIFACTSTAGINGDIRECTORY/screenshots/wasm/nunit-log.txt
export UNO_UITEST_WASM_PROJECT=$BUILD_SOURCESDIRECTORY/testing/TestHarness/TestHarness.Wasm/TestHarness.Wasm.csproj
export UNO_UITEST_WASM_OUTPUT_PATH=$BUILD_SOURCESDIRECTORY/testing/TestHarness/TestHarness.Wasm/bin/Release/net8.0/dist/
export UNO_UITEST_WASM_PROJECT_BUILD_OPTIONS="/p:UnoExtensionsDisableNet7=true /p:GeneratePackageOnBuild=false"

cd $BUILD_SOURCESDIRECTORY

dotnet build -c Release $UNO_UITEST_PROJECT
dotnet build -c Release $UNO_UITEST_WASM_PROJECT /p:IsUiAutomationMappingEnabled=True /p:UseWebAssemblyAOT=false /bl:$BIN_LOG_PATH $UNO_UITEST_WASM_PROJECT_BUILD_OPTIONS

# Start the server
dotnet run --project $UNO_UITEST_WASM_PROJECT -c Release --no-build &

cd $BUILD_SOURCESDIRECTORY/build

npm i chromedriver@119.0.0
npm i puppeteer@21.6.1

mkdir -p $UNO_UITEST_SCREENSHOT_PATH

cd $UNO_UITEST_PROJECT

## Run the tests
dotnet test \
	-c Release \
	-l:"console;verbosity=normal" \
	--logger "nunit;LogFileName=$BUILD_SOURCESDIRECTORY/build/TestResult.xml" \
	-v m \
	|| true
