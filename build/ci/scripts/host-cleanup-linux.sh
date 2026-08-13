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
$SUDO rm -rf /usr/local/lib/android || true
$SUDO rm -rf /opt/ghc || true
$SUDO rm -rf /opt/hostedtoolcache/CodeQL || true

# ── Hosted tool cache (non-.NET runtimes) ──────────────────────────────────
# Note: jobs using NodeTool@0 re-download Node after this. That trade is worth
# it on build jobs; it is why this script should not be added to jobs that only
# validate pre-built binaries.
$SUDO rm -rf /opt/hostedtoolcache/Ruby   || true
$SUDO rm -rf /opt/hostedtoolcache/PyPy   || true
$SUDO rm -rf /opt/hostedtoolcache/Python || true
$SUDO rm -rf /opt/hostedtoolcache/go     || true
$SUDO rm -rf /opt/hostedtoolcache/node   || true

# ── Large apt packages not used in .NET builds ─────────────────────────────
if command -v apt-get >/dev/null 2>&1; then
  # Purge only the explicitly listed packages — no --auto-remove. auto-remove
  # would also pull out orphaned X11 libs (libx11-xcb1, libx11-6) that the
  # Android emulator depends on, and the savings from orphaned deps are
  # negligible next to the packages listed here.
  DEBIAN_FRONTEND=noninteractive timeout 120s \
    $SUDO apt-get purge -y \
      snapd \
      firefox \
      google-chrome-stable \
      'libllvm*' \
      'clang-*' \
      'llvm-*' \
      'php*' \
      'ruby*' \
    2>/dev/null || true
  DEBIAN_FRONTEND=noninteractive timeout 60s $SUDO apt-get clean 2>/dev/null || true
fi

if command -v snap >/dev/null 2>&1; then
  timeout 60s $SUDO snap remove lxd    2>/dev/null || true
  timeout 60s $SUDO snap remove core20 2>/dev/null || true
fi

echo "Disk space after cleanup:"
df -h /
