#!/usr/bin/env bash
# E2E stack'ini ayağa kaldırır ve ortamı besler.
#
# KULLANIM: kaynak olarak çağır —  source scripts/e2e-stack.sh
# (alt kabukta koşarsa export'lar kaybolur ve testler ortam değişkeni bulamaz.)
#
# Kaldırdıkları:
#   Supabase lokal stack  : 54321 (API/GoTrue), 54322 (Postgres), 54323 (Studio)
#   PsikoAl.Api           : 5089
#   PsikoAl.Client (admin): 5057
#
# Durdurmak için:  scripts/e2e-stack-down.sh

set -uo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

# --- 1. Supabase ------------------------------------------------------------
if ! npx --yes supabase status >/dev/null 2>&1; then
  echo "Supabase stack'i kaldırılıyor (ilk koşumda imaj indirir, birkaç dakika sürer)…"
  npx --yes supabase start || { echo "supabase start başarısız" >&2; return 1 2>/dev/null || exit 1; }
fi

status="$(npx --yes supabase status -o env)"
get() { echo "$status" | grep -E "^$1=" | head -1 | cut -d= -f2- | tr -d '"'; }

export E2E_SUPABASE_URL="$(get API_URL)"
export E2E_SUPABASE_ANON_KEY="$(get ANON_KEY)"
export E2E_SUPABASE_SERVICE_ROLE_KEY="$(get SERVICE_ROLE_KEY)"
export E2E_POSTGRES="${E2E_POSTGRES:-Host=127.0.0.1;Port=54322;Database=postgres;Username=postgres;Password=postgres}"

# --- 2. Admin kimliği -------------------------------------------------------
# Kullanıcıyı GoTrue'da ve admin_users tablosunda üreten kod tek yerde:
# src/PsikoAl.Api.E2ETests/SupabaseTestIdentity.cs. Buradaki değerler onunla
# BİREBİR aynı olmak zorunda — ikisi ayrışırsa Playwright login'i 401 alır.
export E2E_ADMIN_EMAIL="e2e-admin@psikoal.test"
export E2E_ADMIN_PASSWORD="E2eAdminPass123!"
export E2E_CLIENT_EMAIL="e2e-client@psikoal.test"
export E2E_CLIENT_PASSWORD="E2eClientPass123!"

export E2E_API_URL="http://localhost:5089"
export ADMIN_URL="http://localhost:5057"

# --- 3. API ve admin paneli -------------------------------------------------
# ASPNETCORE_ENVIRONMENT=Development ŞART: Program.cs yalnız bu ortamda
# RequireHttpsMetadata'yı kapatıyor. Aksi hâlde http Authority'li lokal GoTrue
# yüzünden API /health dahil her istekte 500 döner.
# bind: sürecin DİNLEYECEĞİ adres. health/url: HOST'tan erişilen adres.
# Ayrı olmalarının sebebi Android emülatörü: emülatör host'a 10.0.2.2 üzerinden
# ulaşır ve API yalnız localhost'a bind edilmişse bu bağlantı REDDEDİLİR
# (belirti: mobil akış giriş ekranında takılır, sebep hiçbir yerde görünmez).
# 0.0.0.0'a bind edilince hem host hem emülatör erişir.
start_service() {
  local name="$1" project="$2" bind="$3" health="$4" log="$5" url="${6:-$3}"
  if curl -s -o /dev/null -m 2 "$health"; then
    echo "$name zaten ayakta: $url"
    return 0
  fi
  echo "$name başlatılıyor → $url  (log: $log)"
  ( Supabase__Url="$E2E_SUPABASE_URL" \
    Supabase__AnonKey="$E2E_SUPABASE_ANON_KEY" \
    Supabase__ServiceRoleKey="$E2E_SUPABASE_SERVICE_ROLE_KEY" \
    ConnectionStrings__Postgres="$E2E_POSTGRES" \
    Api__BaseUrl="$E2E_API_URL" \
    ASPNETCORE_ENVIRONMENT=Development \
    dotnet run --project "$project" --no-launch-profile --urls "$bind" >"$log" 2>&1 & )

  for _ in $(seq 1 60); do
    curl -s -o /dev/null -m 2 "$health" && { echo "  hazır."; return 0; }
    sleep 2
  done
  echo "  $name 120 sn içinde ayağa kalkmadı. Log: $log" >&2
  return 1
}

mkdir -p .e2e-logs
# API 0.0.0.0'a bind: Maestro akışları emülatörden 10.0.2.2:5089'a vuruyor.
start_service "PsikoAl.Api" src/PsikoAl.Api "http://0.0.0.0:5089" "$E2E_API_URL/health" .e2e-logs/api.log "$E2E_API_URL"
# Admin paneli yalnız host'tan (Playwright) kullanılıyor, dışa açmaya gerek yok.
start_service "PsikoAl.Client" src/PsikoAl.Client "$ADMIN_URL" "$ADMIN_URL/login" .e2e-logs/client.log

# --- 4. Admin kullanıcısını garanti et --------------------------------------
# .NET E2E testleri kullanıcıyı kendi kurulumunda üretiyor; Playwright üretmiyor.
# Bu adım sırayı bağımsızlaştırır: Playwright tek başına da koşabilsin.
echo "Admin kimliği hazırlanıyor…"
dotnet test src/PsikoAl.Api.E2ETests --filter "Category=E2E" >/dev/null 2>&1 \
  && echo "  API-seviyesi E2E testleri de yeşil." \
  || echo "  UYARI: API-seviyesi E2E testleri kırmızı — detay için: dotnet test src/PsikoAl.Api.E2ETests"

cat <<EOF

E2E stack hazır:
  Supabase   $E2E_SUPABASE_URL   (Studio: http://127.0.0.1:54323)
  API        $E2E_API_URL
  Admin      $ADMIN_URL          ($E2E_ADMIN_EMAIL / $E2E_ADMIN_PASSWORD)
  Mobil hesap                     ($E2E_CLIENT_EMAIL / $E2E_CLIENT_PASSWORD)

Sıradaki:
  dotnet test src/PsikoAl.Api.E2ETests --filter "Category=E2E"   # API zinciri
  cd e2e/admin && npm test                                        # admin paneli (Playwright)
  scripts/e2e-mobile.sh                                           # mobil (Maestro)
EOF
