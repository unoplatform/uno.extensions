#!/usr/bin/env bash
# Reclaims disk space on Linux CI agents by removing pre-installed
# software that is not needed for the build.
#
# This list is based on what the base image contains and
# may need to be adjusted as new software gets installed.
# Use the `du` command to determine what can be uninstalled.

# Use sudo only when available and non-interactive (no password prompt)
if command -v sudo >/dev/null 2>&1 && sudo -n true >/dev/null 2>&1; then
  SUDO="sudo -n"
else
  SUDO=""
fi

echo "Disk space before cleanup:"
df -h /

# ── Docker images — the biggest single win on hosted runners (~30 GB) ───────
# Omitting this is why the runtime-test build jobs still ran out of disk while
# archiving the SDK cache ("tar: Wrote only 8192 of 10240 bytes").
if command -v docker >/dev/null 2>&1; then
  docker system prune -af --volumes 2>/dev/null || true
fi

# ── Home-directory toolchains ──────────────────────────────────────────────
# Wipes ~/.dotnet — safe on ephemeral CI agents only. On a developer machine
# set ALLOW_DOTNET_WIPE=1 to opt in.
if [ "${ALLOW_DOTNET_WIPE:-0}" = "1" ] || [ "${CI:-}" = "true" ] || [ "${TF_BUILD:-}" = "True" ]; then
  rm -rf ~/.cargo ~/.rustup ~/.dotnet || true
else
  echo "Skipping ~/.dotnet wipe — not a CI agent (set ALLOW_DOTNET_WIPE=1 to override)."
  rm -rf ~/.cargo ~/.rustup || true
fi

# ── System-wide pre-installed software not needed for .NET builds ──────────
$SUDO rm -rf /usr/share/swift || true
$SUDO rm -rf /opt/microsoft/msedge || true
$SUDO rm -rf /usr/local/.ghcup || true
$SUDO rm -rf /usr/lib/mono || true
$SUDO rm -rf /opt/ghc || true

# The agent's Android SDK is ~10 GB and worth reclaiming for jobs that don't
# build Android - but a job that does must keep it, or the build fails with
# XA5300 ("The Android SDK directory could not be found"). Set
# KEEP_ANDROID_SDK=1 in such a job.
if [ "${KEEP_ANDROID_SDK:-0}" = "1" ]; then
  echo "Keeping /usr/local/lib/android (KEEP_ANDROID_SDK=1)."
else
  $SUDO rm -rf /usr/local/lib/android || true
fi
$SUDO rm -rf /opt/hostedtoolcache/CodeQL || true

# ── Hosted tool cache (non-.NET runtimes) ──────────────────────────────────
# Node is deliberately left in place: several stages use NodeTool@0 and would
# just re-download it.
$SUDO rm -rf /opt/hostedtoolcache/Ruby   || true
$SUDO rm -rf /opt/hostedtoolcache/PyPy   || true
$SUDO rm -rf /opt/hostedtoolcache/Python || true
$SUDO rm -rf /opt/hostedtoolcache/go     || true

# ── apt ────────────────────────────────────────────────────────────────────
# Only snapd is purged here, plus a cache clean.
#
# studio.live's version of this script additionally purges firefox,
# google-chrome-stable, libllvm*, clang-*, llvm-* and php*/ruby*. Do NOT copy
# that list into this repo:
#
#   * Purging `libllvm*` cascades. apt removes reverse-dependencies even
#     without --auto-remove, and Mesa's software rasteriser (llvmpipe) links
#     LLVM, so `xvfb` goes with it. Both Skia desktop stages here run their
#     tests under `xvfb-run`, which then fails with exit 127
#     ("xvfb-run: command not found") - build 227556.
#   * `google-chrome-stable` is needed by the WebAssembly UI tests, which drive
#     a real Chrome through chromedriver.
#
# The Docker prune above already reclaims far more than this list would.
if command -v apt-get >/dev/null 2>&1; then
  DEBIAN_FRONTEND=noninteractive timeout 120s $SUDO apt-get purge -y snapd 2>/dev/null || true
  DEBIAN_FRONTEND=noninteractive timeout 60s $SUDO apt-get clean 2>/dev/null || true
fi

if command -v snap >/dev/null 2>&1; then
  timeout 60s $SUDO snap remove lxd    2>/dev/null || true
  timeout 60s $SUDO snap remove core20 2>/dev/null || true
fi

echo "Disk space after cleanup:"
df -h /
