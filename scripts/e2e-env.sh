#!/usr/bin/env bash
# Lokal Supabase stack'inin anahtarlarını E2E testlerinin beklediği değişkenlere yazar.
#
# KULLANIM: kaynak olarak çağır, çalıştırma —  `source scripts/e2e-env.sh`
# (alt kabukta çalıştırılırsa export'lar kaybolur ve testler "ortam değişkeni
#  tanımsız" hatasıyla düşer.)
#
# Anahtarlar neden sabit yazılmıyor: Supabase CLI sürümleri arasında lokal anahtar
# formatı değişti (legacy JWT -> sb_publishable_/sb_secret_). Sabit değer, CLI
# güncellendiği gün testleri 401 ile kırar ve hata sözleşme hatası sanılır.

set -euo pipefail

if ! status="$(npx --yes supabase status -o env 2>/dev/null)"; then
  echo "Lokal Supabase stack'i ayakta değil. Önce: npx supabase start" >&2
  return 1 2>/dev/null || exit 1
fi

get() { echo "$status" | grep -E "^$1=" | head -1 | cut -d= -f2- | tr -d '"'; }

export E2E_SUPABASE_URL="$(get API_URL)"
export E2E_SUPABASE_ANON_KEY="$(get ANON_KEY)"
export E2E_SUPABASE_SERVICE_ROLE_KEY="$(get SERVICE_ROLE_KEY)"
export E2E_POSTGRES="${E2E_POSTGRES:-Host=127.0.0.1;Port=54322;Database=postgres;Username=postgres;Password=postgres}"

echo "E2E ortamı hazır:"
echo "  E2E_SUPABASE_URL = $E2E_SUPABASE_URL"
echo "  E2E_SUPABASE_ANON_KEY = ${E2E_SUPABASE_ANON_KEY:0:12}…"
echo "  E2E_SUPABASE_SERVICE_ROLE_KEY = ${E2E_SUPABASE_SERVICE_ROLE_KEY:0:12}…"
