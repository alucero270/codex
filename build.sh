#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
web_root="$repo_root/src/Codex.Web"
compose_file="$repo_root/ops/docker-compose.yml"
env_file="$repo_root/.env"
api_url="${STRATA_BUILD_API_URL:-http://127.0.0.1:5180}"

run_step() {
  local name="$1"
  shift

  echo "==> $name"
  "$@"
  echo "OK: $name"
}

install_web_dependencies() {
  (
    cd "$web_root"
    npm install
  )
}

build_web_shell() {
  (
    cd "$web_root"
    npm run build
  )
}

probe_api_readiness() {
  local api_pid
  local api_log
  api_log="$(mktemp)"

  cleanup() {
    if [[ -n "${api_pid:-}" ]] && kill -0 "$api_pid" 2>/dev/null; then
      kill "$api_pid"
      wait "$api_pid" || true
    fi

    rm -f "$api_log"
  }

  trap cleanup RETURN

  (
    cd "$repo_root"
    ASPNETCORE_ENVIRONMENT=Development \
    ASPNETCORE_URLS="$api_url" \
    Codex__DocsRootPath="$repo_root/docs" \
    ConnectionStrings__Default="Host=localhost;Port=5432;Database=strata;Username=strata;Password=strata" \
    dotnet run --project src/Codex.Api/Codex.Api.csproj --no-build --no-launch-profile
  ) >"$api_log" 2>&1 &
  api_pid=$!

  for _ in $(seq 1 30); do
    if curl --fail --silent "$api_url/health" >/dev/null; then
      return 0
    fi

    if ! kill -0 "$api_pid" 2>/dev/null; then
      cat "$api_log"
      echo "API process exited before readiness succeeded." >&2
      return 1
    fi

    sleep 2
  done

  cat "$api_log"
  echo "API readiness probe did not succeed at $api_url/health." >&2
  return 1
}

echo "Running Strata baseline validation bootstrap..."

run_step "Build .NET solution" dotnet build "$repo_root/Codex.slnx"

run_step "Install web dependencies" install_web_dependencies
run_step "Build web shell" build_web_shell

if [[ ! -f "$env_file" ]]; then
  echo "Expected repository-root .env for Compose validation." >&2
  exit 1
fi

run_step "Validate Docker Compose config" docker compose -f "$compose_file" --env-file "$env_file" config
run_step "Probe API readiness" probe_api_readiness

echo "Platform readiness validation passed."
