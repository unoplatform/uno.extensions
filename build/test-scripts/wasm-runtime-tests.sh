#!/bin/bash
# Run Uno.Extensions runtime tests in a headless browser against a published WebAssembly build.
#
# Unlike the mobile heads this uses the engine's own Playwright-based runner tool, which serves the
# published app, drives it with query parameters and writes the NUnit document itself - so no
# in-app autostart is involved.
set -euo pipefail
IFS=$'\n\t'

: "${BUILD_SOURCESDIRECTORY:?BUILD_SOURCESDIRECTORY is required}"
: "${RuntimeTestsArtifactPath:?RuntimeTestsArtifactPath is required}"

# Keep in sync with the Uno.UI.RuntimeTests.Engine version the app builds against; a runner older
# than the engine silently fails to find the harness.
runner_version="${UNO_WASM_RUNTIME_TESTS_RUNNER_VERSION:-2.0.0-dev.79}"

build_root="${BUILD_SOURCESDIRECTORY}/build"
results_dir="${build_root}/wasm-runtime-tests"
logs_dir="${results_dir}/logs"
results_path="${results_dir}/wasm-runtime-tests-results.xml"
runner_log_path="${logs_dir}/wasm-runtime-tests-runner.log"
tool_path="${build_root}/.dotnet-tools"
playwright_root="${build_root}/playwright-browsers"

mkdir -p "${results_dir}" "${logs_dir}" "${tool_path}" "${playwright_root}"

if [[ ! -d "${RuntimeTestsArtifactPath}" ]]; then
  echo "ERROR: artifact directory '${RuntimeTestsArtifactPath}' was not found." >&2
  exit 1
fi

if dotnet tool list --tool-path "${tool_path}" 2>/dev/null | grep -q "uno.ui.runtimetests.engine.wasm.runner"; then
  dotnet tool update --tool-path "${tool_path}" Uno.UI.RuntimeTests.Engine.Wasm.Runner --version "${runner_version}"
else
  dotnet tool install --tool-path "${tool_path}" Uno.UI.RuntimeTests.Engine.Wasm.Runner --version "${runner_version}"
fi

export PATH="${tool_path}:${PATH}"
export PLAYWRIGHT_BROWSERS_PATH="${playwright_root}"

pushd "${RuntimeTestsArtifactPath}" >/dev/null
npx playwright install chromium
popd >/dev/null

{
  echo "=== wasm runtime tests ==="
  echo "App path:       ${RuntimeTestsArtifactPath}"
  echo "Results path:   ${results_path}"
  echo "Runner version: ${runner_version}"
  echo "=========================="
} | tee "${runner_log_path}"

# Hot-reload tests need a build environment the browser doesn't have; the desktop stage owns them.
uno-runtimetests-wasm \
  --app-path "${RuntimeTestsArtifactPath}" \
  --output "${results_path}" \
  --timeout 1800 \
  --query-param 'UNO_RUNTIME_TESTS_RUN_TESTS={"Filter":{"Value":"!_HotReload"}}' \
  --browser-log-level verbose \
  2>&1 | tee -a "${runner_log_path}"

if [[ ! -f "${results_path}" ]]; then
  echo "ERROR: runtime tests did not produce results at ${results_path}" >&2
  exit 1
fi

RUNTIME_TEST_RESULTS="${results_path}" python3 "${BUILD_SOURCESDIRECTORY}/build/test-scripts/validate-runtime-test-results.py"
