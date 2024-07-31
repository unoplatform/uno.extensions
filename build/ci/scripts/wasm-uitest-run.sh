#!/bin/bash
set -euo pipefail
IFS=$'\n\t'

export UNO_UITEST_TARGETURI=http://localhost:5000
export UNO_UITEST_DRIVERPATH_CHROME=$BUILD_SOURCESDIRECTORY/build/node_modules/chromedriver/lib/chromedriver
export UNO_UITEST_CHROME_BINARY_PATH=~/.cache/puppeteer/chrome/linux-127.0.6533.72/chrome-linux64/chrome
export UNO_UITEST_SCREENSHOT_PATH=$BUILD_ARTIFACTSTAGINGDIRECTORY/screenshots/wasm
export BIN_LOG_PATH=$BUILD_ARTIFACTSTAGINGDIRECTORY/wasm-uitest.binlog
export UNO_UITEST_PLATFORM=Browser
export UNO_UITEST_CHROME_CONTAINER_MODE=true
export UNO_UITEST_PROJECT=$BUILD_SOURCESDIRECTORY/testing/TestHarness/TestHarness.UITest
export UNO_UITEST_LOGFILE=$BUILD_ARTIFACTSTAGINGDIRECTORY/screenshots/wasm/nunit-log.txt
export UNO_UITEST_WASM_SOLUTION=$BUILD_SOURCESDIRECTORY/testing/TestHarness/TestHarness.sln
export UNO_UITEST_WASM_PROJECT=$BUILD_SOURCESDIRECTORY/testing/TestHarness/TestHarness/TestHarness.csproj
export UNO_UITEST_WASM_OUTPUT_PATH=$BUILD_SOURCESDIRECTORY/testing/TestHarness/TestHarness/bin/Debug/net8.0-browserwasm/dist/
export UNO_UITEST_WASM_PROJECT_BUILD_OPTIONS=" /p:Build_Android=false /p:Build_iOS=false /p:Build_Windows=false /p:Build_Desktop=false /p:GeneratePackageOnBuild=false"

cd $BUILD_SOURCESDIRECTORY

dotnet build -c Debug $UNO_UITEST_WASM_SOLUTION /p:IsUiAutomationMappingEnabled=True /p:UseWebAssemblyAOT=false /p:Build_MacCatalyst=false /p:Build_Android=false /p:Build_iOS=false /p:Build_Windows=false /p:Build_Desktop=false /p:GeneratePackageOnBuild=false /bl:$BIN_LOG_PATH

# Start the server
dotnet run --project $UNO_UITEST_WASM_PROJECT -f net8.0-browserwasm /p:Build_MacCatalyst=false /p:Build_Android=false /p:Build_iOS=false /p:Build_Windows=false /p:Build_Desktop=false  -c Debug --no-build &

cd $BUILD_SOURCESDIRECTORY/build

npm i chromedriver@127.0.0
npm i puppeteer@22.14.0

mkdir -p $UNO_UITEST_SCREENSHOT_PATH

cd $UNO_UITEST_PROJECT

## Run the tests
dotnet test \
	-c Debug \
	-l:"console;verbosity=normal" \
	--logger "nunit;LogFileName=$BUILD_SOURCESDIRECTORY/build/TestResult.xml" \
	-v m \
	|| true
