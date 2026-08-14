#!/bin/bash
# Run Uno.Extensions runtime tests on an iOS simulator.
#
# Ported from Studio Live's build/test-scripts/ios-simulator-runtime-tests.sh. Mechanism:
#   * iOS simulators share the host's /tmp, so the result file path is passed through
#     SIMCTL_CHILD_* and polled directly on the host filesystem - no HTTP listener needed.
#   * We pass UITEST_RUNTIME_AUTOSTART_RESULT_FILE, NOT UNO_RUNTIME_TESTS_RUN_TESTS. The latter
#     wakes the engine's embedded-runner ModuleInitializer, which calls Console.CancelKeyPress and
#     throws PlatformNotSupportedException inside a sandboxed iOS app.
#   * MobileRuntimeTestsAutostart drives UnitTestsControl directly and writes the NUnit document.
set -euo pipefail
IFS=$'\n\t'

: "${BUILD_SOURCESDIRECTORY:?BUILD_SOURCESDIRECTORY is required}"
: "${RuntimeTestsArtifactPath:?RuntimeTestsArtifactPath is required}"

# Device name and runtime must both exist on the agent image, and the runtime must match the SDK
# the .app was linked against in the build stage. Studio Live hit repeated failures here: a device
# that isn't pre-installed makes the selector below exit non-zero inside a command substitution,
# so `set -e` kills the script *before* the diagnostic prints. Keep them centralized in the YAML.
SIMULATOR_DEVICE="${SIMULATOR_DEVICE:-iPhone 16}"
SIMULATOR_RUNTIME_PREFIX="${SIMULATOR_RUNTIME_PREFIX:-com.apple.CoreSimulator.SimRuntime.iOS-18}"
TEST_FILTER="${TEST_FILTER:-!_HotReload}"
RUN_TIMEOUT_SECONDS="${RUN_TIMEOUT_SECONDS:-1800}"
# BUNDLE_ID is read from the .app's Info.plist below rather than hard-coded.

build_root="${BUILD_SOURCESDIRECTORY}/build"
results_dir="${build_root}/ios-runtime-tests"
logs_dir="${results_dir}/logs"
launch_log="${logs_dir}/simulator-launch.log"
app_log="${logs_dir}/app-log.txt"
device_log_archive="${logs_dir}/DeviceLog.logarchive"
timestamp="$(date +%Y%m%d%H%M%S)"
result_file="/tmp/uno-extensions-rt-${timestamp}.xml"
results_path="${results_dir}/ios-runtime-tests-results.xml"

mkdir -p "${results_dir}" "${logs_dir}"
rm -f "${result_file}"

app_bundle="$(find "${RuntimeTestsArtifactPath}" -type d -name '*.app' -print | head -n 1 || true)"
if [[ -z "${app_bundle}" ]]; then
  echo "ERROR: no .app bundle found under ${RuntimeTestsArtifactPath}" >&2
  exit 1
fi
echo "Using app bundle: ${app_bundle}"

if [[ -z "${BUNDLE_ID:-}" ]]; then
  info_plist="${app_bundle}/Info.plist"
  if [[ ! -f "${info_plist}" ]]; then
    echo "ERROR: Info.plist not found at ${info_plist}" >&2
    exit 1
  fi
  # `defaults read` handles the binary plist that dotnet publish emits; PlistBuddy is the fallback.
  BUNDLE_ID="$(defaults read "${app_bundle}/Info" CFBundleIdentifier 2>/dev/null \
    || /usr/libexec/PlistBuddy -c 'Print :CFBundleIdentifier' "${info_plist}" 2>/dev/null \
    || true)"
  if [[ -z "${BUNDLE_ID}" ]]; then
    echo "ERROR: could not read CFBundleIdentifier from ${info_plist}" >&2
    exit 1
  fi
fi
echo "Bundle identifier: ${BUNDLE_ID}"

device_udid="$(xcrun simctl list devices --json | python3 -c "
import json, sys
devices = json.load(sys.stdin)['devices']
prefix, name = '${SIMULATOR_RUNTIME_PREFIX}', '${SIMULATOR_DEVICE}'
for runtime, entries in devices.items():
    if not runtime.startswith(prefix):
        continue
    for device in entries:
        if device.get('isAvailable') is not False and device.get('name') == name:
            print(device['udid'])
            sys.exit(0)
sys.exit(1)
" || true)"

if [[ -z "${device_udid}" ]]; then
  echo "ERROR: no available simulator for device='${SIMULATOR_DEVICE}' runtime prefix='${SIMULATOR_RUNTIME_PREFIX}'" >&2
  xcrun simctl list devices available >&2
  exit 1
fi
echo "Selected simulator UDID: ${device_udid}"

cleanup() {
  if [[ -n "${device_udid:-}" ]]; then
    # Managed Console.WriteLine and Apple's own launch diagnostics all land in os_log; without
    # collecting it, a CI failure leaves nothing but the pre-pid simctl output.
    xcrun simctl spawn "${device_udid}" log show \
      --last 30m --style compact \
      --predicate "processImagePath CONTAINS \"Uno.Extensions.RuntimeTests\"" \
      >"${app_log}" 2>&1 || true
    xcrun simctl spawn "${device_udid}" log collect --output "${device_log_archive}" 2>/dev/null || true
  fi
  xcrun simctl terminate "${device_udid}" "${BUNDLE_ID}" 2>/dev/null || true
  xcrun simctl shutdown "${device_udid}" 2>/dev/null || true
}
trap cleanup EXIT

# Fire-and-forget boot. Deliberately NOT `simctl bootstatus -b`: that blocks until every
# CoreSimulator data-migration plugin finishes, 5+ minutes on a fresh hosted mac runner. The
# "Booted" state lands long before those settle, and install/launch wait only as long as needed.
xcrun simctl boot "${device_udid}" || true

boot_deadline=$((SECONDS + 180))
state=""
while [[ ${SECONDS} -lt ${boot_deadline} ]]; do
  state="$(xcrun simctl list devices --json | python3 -c "
import json, sys
for _, entries in json.load(sys.stdin)['devices'].items():
    for device in entries:
        if device.get('udid') == '${device_udid}':
            print(device.get('state', ''))
            sys.exit(0)
" 2>/dev/null || true)"
  if [[ "${state}" == "Booted" ]]; then
    break
  fi
  sleep 2
done
if [[ "${state}" != "Booted" ]]; then
  echo "ERROR: simulator ${device_udid} did not reach Booted within 180s (last=${state:-unknown})" >&2
  exit 1
fi
echo "Simulator booted in ${SECONDS}s."

xcrun simctl install "${device_udid}" "${app_bundle}"

# Write the results into the app's own data container rather than /tmp. Simulator apps run
# natively, so the container path `simctl get_app_container` reports is the same path the app sees
# and is directly readable from the host - no assumption about whether the simulator shares /tmp.
app_container="$(xcrun simctl get_app_container "${device_udid}" "${BUNDLE_ID}" data 2>/dev/null || true)"
if [[ -n "${app_container}" && -d "${app_container}" ]]; then
  result_file="${app_container}/Documents/uno-extensions-rt-${timestamp}.xml"
  mkdir -p "${app_container}/Documents"
  echo "Using app data container for results: ${result_file}"
else
  echo "WARNING: could not resolve the app data container; falling back to ${result_file}"
fi
rm -f "${result_file}"

# macOS base64 uses -b; GNU uses -w.
encoded_filter="$(printf '%s' "${TEST_FILTER}" | base64 -b 0 2>/dev/null || printf '%s' "${TEST_FILTER}" | base64 -w 0)"

export SIMCTL_CHILD_UITEST_RUNTIME_AUTOSTART_RESULT_FILE="${result_file}"
export SIMCTL_CHILD_UITEST_RUNTIME_TESTS_FILTER="${encoded_filter}"

echo "Launching ${BUNDLE_ID} (result=${result_file}, filter='${TEST_FILTER}')..."
xcrun simctl launch --terminate-running-process "${device_udid}" "${BUNDLE_ID}" >"${launch_log}" 2>&1 || {
  echo "ERROR: simctl launch failed." >&2
  cat "${launch_log}" >&2 || true
  exit 1
}
cat "${launch_log}"

echo "Waiting up to ${RUN_TIMEOUT_SECONDS}s for ${result_file}..."
SECONDS=0
while [[ ${SECONDS} -lt ${RUN_TIMEOUT_SECONDS} ]]; do
  if [[ -f "${result_file}" ]]; then
    break
  fi
  sleep 5
done

if [[ ! -f "${result_file}" ]]; then
  echo "ERROR: ${result_file} did not appear within ${RUN_TIMEOUT_SECONDS}s." >&2
  exit 1
fi

cp "${result_file}" "${results_path}"
echo "Copied ${result_file} -> ${results_path}"

RUNTIME_TEST_RESULTS="${results_path}" python3 "${BUILD_SOURCESDIRECTORY}/build/test-scripts/validate-runtime-test-results.py"
