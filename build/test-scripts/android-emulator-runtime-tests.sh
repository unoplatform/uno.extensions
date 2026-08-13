#!/bin/bash
# Run Uno.Extensions runtime tests on an Android emulator.
#
# Ported from Studio Live's build/test-scripts/android-emulator-runtime-tests.sh, which in turn
# mirrors uno core's android-run-skia-runtime-tests.sh. Mechanism:
#   * `adb shell am start -e <key> <value>` passes intent extras. MainActivity.OnCreate bridges the
#     UITEST_RUNTIME_* keys into Environment.SetEnvironmentVariable before base.OnCreate, so the
#     managed code sees them as process env vars (Android has no way to set them directly).
#   * The result file lives in the app's external-storage scratch dir; we poll with `adb shell test
#     -e`, then `adb pull` it.
#   * We deliberately do NOT set UNO_RUNTIME_TESTS_RUN_TESTS. That would wake the Uno engine's
#     embedded-runner ModuleInitializer, which hooks Console.CancelKeyPress - fine here but broken
#     on iOS, and keeping both mobile heads on one mechanism is worth more than the shortcut.
set -euo pipefail
IFS=$'\n\t'

: "${BUILD_SOURCESDIRECTORY:?BUILD_SOURCESDIRECTORY is required}"
: "${RuntimeTestsArtifactPath:?RuntimeTestsArtifactPath is required}"

AVD_NAME="${AVD_NAME:-uno-extensions-rt-avd}"
AVD_IMAGE="${AVD_IMAGE:-system-images;android-34;default;x86_64}"
AVD_DEVICE="${AVD_DEVICE:-pixel_6}"
EMULATOR_BOOT_TIMEOUT="${EMULATOR_BOOT_TIMEOUT:-600}"
RUN_TIMEOUT_SECONDS="${RUN_TIMEOUT_SECONDS:-1800}"
# Hot-reload tests need a build environment the emulator doesn't have; the desktop stage owns them.
TEST_FILTER="${TEST_FILTER:-!_HotReload}"
# PACKAGE_NAME is intentionally not defaulted - it is read from the APK below, so the script
# survives an ApplicationId change in the csproj. Override only to launch a sibling package.

build_root="${BUILD_SOURCESDIRECTORY}/build"
results_dir="${build_root}/android-runtime-tests"
logs_dir="${results_dir}/logs"
emulator_log="${logs_dir}/emulator.log"
logcat_log="${logs_dir}/logcat.log"
timestamp="$(date +%Y%m%d%H%M%S)"
results_path="${results_dir}/android-runtime-tests-results.xml"

mkdir -p "${results_dir}" "${logs_dir}"

apk_path="$(find "${RuntimeTestsArtifactPath}" -maxdepth 6 -type f \( -name '*-Signed.apk' -o -name '*.apk' \) -print | head -n 1 || true)"
if [[ -z "${apk_path}" ]]; then
  echo "ERROR: no APK found under ${RuntimeTestsArtifactPath}" >&2
  exit 1
fi
echo "Using APK: ${apk_path}"

# Provision a writable SDK under the build dir. The hosted ubuntu image ships an SDK with only
# versioned cmdline-tools/<ver>/bin, not cmdline-tools/latest/bin, so a generic PATH export finds
# nothing. Owning our own location keeps this self-contained.
export ANDROID_HOME="${build_root}/android-sdk"
export ANDROID_SDK_ROOT="${ANDROID_HOME}"

if [[ ! -x "${ANDROID_HOME}/cmdline-tools/latest/bin/sdkmanager" ]]; then
  echo "Provisioning Android cmdline-tools under ${ANDROID_HOME}..."
  mkdir -p "${ANDROID_HOME}/cmdline-tools"
  cmdline_tools_zip="commandlinetools-linux-11076708_latest.zip"
  tmp_zip="$(mktemp -d)/${cmdline_tools_zip}"
  curl -fsSL "https://dl.google.com/android/repository/${cmdline_tools_zip}" -o "${tmp_zip}"
  unzip -q "${tmp_zip}" -d "${ANDROID_HOME}/cmdline-tools"
  rm -f "${tmp_zip}"
  # The zip extracts to cmdline-tools/cmdline-tools/...; sdkmanager expects cmdline-tools/latest/.
  mv "${ANDROID_HOME}/cmdline-tools/cmdline-tools" "${ANDROID_HOME}/cmdline-tools/latest"
fi

SDKMANAGER="${ANDROID_HOME}/cmdline-tools/latest/bin/sdkmanager"
AVDMANAGER="${ANDROID_HOME}/cmdline-tools/latest/bin/avdmanager"
export PATH="${ANDROID_HOME}/platform-tools:${ANDROID_HOME}/emulator:${ANDROID_HOME}/cmdline-tools/latest/bin:${PATH}"

yes | "${SDKMANAGER}" --sdk_root="${ANDROID_HOME}" --licenses >/dev/null 2>&1 || true
"${SDKMANAGER}" --sdk_root="${ANDROID_HOME}" --install "${AVD_IMAGE}" "platform-tools" "emulator" "build-tools;34.0.0" >/dev/null

echo "no" | "${AVDMANAGER}" create avd -n "${AVD_NAME}" -k "${AVD_IMAGE}" --device "${AVD_DEVICE}" --force >/dev/null

# Read package name + launcher activity from the APK so the script stays in sync with whatever
# ApplicationId the csproj produced.
aapt_path="$(find "${ANDROID_HOME}/build-tools" -maxdepth 2 -type f -name aapt -print 2>/dev/null | sort | tail -n 1 || true)"
if [[ -z "${aapt_path}" ]]; then
  echo "ERROR: aapt not found under ${ANDROID_HOME}/build-tools" >&2
  exit 1
fi
badging="$("${aapt_path}" dump badging "${apk_path}" 2>/dev/null || true)"
if [[ -z "${PACKAGE_NAME:-}" ]]; then
  PACKAGE_NAME="$(printf '%s\n' "${badging}" | awk -F"'" '/^package: / {print $2; exit}')"
  if [[ -z "${PACKAGE_NAME}" ]]; then
    echo "ERROR: could not read package name from ${apk_path}" >&2
    exit 1
  fi
fi
launcher_activity="$(printf '%s\n' "${badging}" | awk -F"'" '/launchable-activity/ {print $2; exit}')"
if [[ -z "${launcher_activity}" ]]; then
  echo "ERROR: could not resolve launcher activity from ${apk_path}" >&2
  exit 1
fi
echo "Package name: ${PACKAGE_NAME}"
echo "Launcher activity: ${launcher_activity}"
device_result_file="/storage/emulated/0/Android/data/${PACKAGE_NAME}/files/uno-extensions-rt-${timestamp}.xml"

adb start-server

EMULATOR_PID=""
LOGCAT_PID=""

cleanup() {
  if [[ -n "${LOGCAT_PID}" ]]; then
    kill "${LOGCAT_PID}" 2>/dev/null || true
    wait "${LOGCAT_PID}" 2>/dev/null || true
  fi
  if [[ -n "${EMULATOR_PID}" ]]; then
    adb -s emulator-5554 emu kill 2>/dev/null || true
    wait "${EMULATOR_PID}" 2>/dev/null || true
  fi
}
trap cleanup EXIT

emulator -avd "${AVD_NAME}" \
  -no-window -no-audio -no-boot-anim \
  -no-snapshot -wipe-data \
  -gpu swiftshader_indirect \
  -accel on \
  -port 5554 \
  >"${emulator_log}" 2>&1 &
EMULATOR_PID=$!
echo "Emulator starting (pid=${EMULATOR_PID}, log=${emulator_log})"

adb wait-for-device
echo "Waiting for emulator boot (timeout=${EMULATOR_BOOT_TIMEOUT}s)..."
SECONDS=0
while true; do
  if [[ ${SECONDS} -ge ${EMULATOR_BOOT_TIMEOUT} ]]; then
    echo "ERROR: emulator did not boot within ${EMULATOR_BOOT_TIMEOUT}s" >&2
    exit 1
  fi
  if [[ "$(adb shell getprop sys.boot_completed 2>/dev/null | tr -d '\r')" == "1" ]]; then
    break
  fi
  sleep 2
done
echo "Emulator booted in ${SECONDS}s."

# Stop animations racing the tests.
adb shell settings put global window_animation_scale 0 || true
adb shell settings put global transition_animation_scale 0 || true
adb shell settings put global animator_duration_scale 0 || true

adb logcat -c || true
adb logcat >"${logcat_log}" 2>&1 &
LOGCAT_PID=$!

adb install -r "${apk_path}"

# Needed on pre-scoped-storage API levels to write the result file; no-op on newer ones.
adb shell pm grant "${PACKAGE_NAME}" android.permission.WRITE_EXTERNAL_STORAGE 2>/dev/null || true
adb shell pm grant "${PACKAGE_NAME}" android.permission.READ_EXTERNAL_STORAGE 2>/dev/null || true

# Base64 so the filter survives `am start -e` quoting rules.
encoded_filter="$(printf '%s' "${TEST_FILTER}" | base64 -w 0)"

echo "Starting ${PACKAGE_NAME} (result=${device_result_file}, filter='${TEST_FILTER}')..."
# No `monkey -p` fallback: monkey launches the activity but cannot forward -e extras, so the
# in-app autostart would never see the result-file variable and this would wait out the full
# timeout for a file that never appears. Failing fast surfaces the real cause.
am_start_output="$(
  adb shell am start \
    -a android.intent.action.MAIN \
    -c android.intent.category.LAUNCHER \
    -e UITEST_RUNTIME_AUTOSTART_RESULT_FILE "${device_result_file}" \
    -e UITEST_RUNTIME_TESTS_FILTER "${encoded_filter}" \
    -n "${PACKAGE_NAME}/${launcher_activity}" \
    2>&1
)" || {
  echo "ERROR: am start failed for ${PACKAGE_NAME}/${launcher_activity}." >&2
  printf '%s\n' "${am_start_output:-}" >&2
  adb shell cmd package resolve-activity --brief "${PACKAGE_NAME}" >&2 || true
  exit 1
}
printf '%s\n' "${am_start_output}"

echo "Waiting up to ${RUN_TIMEOUT_SECONDS}s for ${device_result_file}..."
SECONDS=0
while [[ ${SECONDS} -lt ${RUN_TIMEOUT_SECONDS} ]]; do
  if adb shell "[ -f \"${device_result_file}\" ] && echo found || true" 2>/dev/null | tr -d '\r' | grep -q '^found$'; then
    break
  fi
  sleep 5
  if ! adb shell ps 2>/dev/null | grep -q "${PACKAGE_NAME}"; then
    echo "App process is gone; breaking out of the wait loop."
    break
  fi
done

if ! adb shell "[ -f \"${device_result_file}\" ] && echo found || true" 2>/dev/null | tr -d '\r' | grep -q '^found$'; then
  echo "ERROR: ${device_result_file} did not appear within ${RUN_TIMEOUT_SECONDS}s." >&2
  echo "--- emulator log (tail) ---" >&2
  tail -n 200 "${emulator_log}" >&2 || true
  echo "--- logcat (tail) ---" >&2
  tail -n 400 "${logcat_log}" >&2 || true
  exit 1
fi

adb pull "${device_result_file}" "${results_path}"
echo "Pulled ${device_result_file} -> ${results_path}"

RUNTIME_TEST_RESULTS="${results_path}" python3 "${BUILD_SOURCESDIRECTORY}/build/test-scripts/validate-runtime-test-results.py"
