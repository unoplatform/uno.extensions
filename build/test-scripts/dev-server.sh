# Sourced by the mobile runtime-test scripts for their hot-reload lanes.
#
# Starts the Uno DevServer that HotReloadHelper sends its file edits to, and waits until it accepts
# connections: the app connects once at startup and does not retry a refused first attempt. The app
# has the endpoint baked in at build time (UnoRemoteControlHost/Port in the lane's
# DebugPlatforms.props), so only the port has to agree.
#
# Requires DEV_SERVER_HOST_DLL (path to Uno.UI.RemoteControl.Host.dll) and DEV_SERVER_PORT.
start_dev_server() {
  local log_file="$1"
  : "${DEV_SERVER_HOST_DLL:?DEV_SERVER_HOST_DLL is required when DEV_SERVER_PORT is set}"

  # Started from its own directory, as the runtime-tests engine does on desktop. --ppid ties its
  # lifetime to this script, so no cleanup is needed.
  (cd "$(dirname "${DEV_SERVER_HOST_DLL}")" \
    && exec dotnet "${DEV_SERVER_HOST_DLL}" --httpPort "${DEV_SERVER_PORT}" --ppid $$ --metadata-updates true) \
    >"${log_file}" 2>&1 &
  local pid=$!

  local attempt
  for attempt in $(seq 1 60); do
    # Any HTTP answer, even a 404, means the port is bound.
    if curl -s -o /dev/null "http://127.0.0.1:${DEV_SERVER_PORT}/"; then
      echo "DevServer listening on port ${DEV_SERVER_PORT} after ${attempt}s (pid=${pid}, log=${log_file})."
      return 0
    fi
    kill -0 "${pid}" 2>/dev/null || break
    sleep 1
  done

  echo "ERROR: the DevServer did not start listening on port ${DEV_SERVER_PORT}." >&2
  tail -n 100 "${log_file}" >&2 || true
  exit 1
}
