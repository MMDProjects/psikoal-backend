#!/usr/bin/env bash
# E2E stack'ini durdurur. Supabase verisi KORUNUR (--no-backup verilmez);
# tamamen sıfırlamak için: npx supabase db reset
set -uo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

echo "API ve admin paneli durduruluyor…"
# Yalnız bu iki portu dinleyen süreçler hedeflenir — makinedeki diğer dotnet
# süreçlerine dokunulmaz.
for port in 5089 5057; do
  pid="$(netstat -ano 2>/dev/null | grep -E "LISTENING" | grep -E ":$port\b" | awk '{print $NF}' | head -1)"
  if [ -n "${pid:-}" ]; then
    echo "  port $port → PID $pid"
    taskkill //PID "$pid" //F >/dev/null 2>&1 || kill -9 "$pid" 2>/dev/null || true
  fi
done

echo "Supabase durduruluyor…"
npx --yes supabase stop
