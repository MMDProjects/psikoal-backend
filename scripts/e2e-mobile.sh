#!/usr/bin/env bash
# Maestro akislarini kosar.
#
# KULLANIM:
#   source scripts/e2e-stack.sh           # stack + ortam degiskenleri
#   (cd e2e/admin && npx playwright test)  # zincirin ilk halkasi, slug'i uretir
#   scripts/e2e-mobile.sh                  # mobil halka
#
# SECENEKLER (ortam degiskeni):
#   PSIKOAL_APP_DIR  mobil repo yolu (varsayilan: ../native-atomic)
#   MAESTRO_APP_ID   paket adi (varsayilan: derlenmis APK'dan tespit edilir)
#   E2E_SKIP_INSTALL APK kurulumunu atla (zaten kuruluysa)
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

APP_DIR="${PSIKOAL_APP_DIR:-$ROOT/../native-atomic}"
SLUG_FILE="e2e/admin/.artifacts/category-slug.txt"
API_PORT="${E2E_API_PORT:-5089}"

die() { echo "HATA: $*" >&2; exit 1; }

# --- Araclar ----------------------------------------------------------------
command -v maestro >/dev/null 2>&1 \
  || die "maestro bulunamadi. Kurulum: https://docs.maestro.dev/getting-started/installing-maestro"

ADB="adb"
if ! command -v adb >/dev/null 2>&1; then
  ADB="${ANDROID_HOME:-${ANDROID_SDK_ROOT:-}}/platform-tools/adb"
  { [ -x "$ADB" ] || [ -x "$ADB.exe" ]; } || die "adb bulunamadi. ANDROID_HOME tanimli mi?"
fi

# --- Cihaz ------------------------------------------------------------------
"$ADB" devices | grep -qE "\sdevice$" \
  || die "Bagli cihaz/emulator yok. Baslatin:
    ANDROID_AVD_HOME=<avd-dizini> emulator -avd psikoal-e2e"

# `adb devices` cihazi "device" gosterse de sistem hazir olmayabilir; boot
# bitmeden APK kurulumu "Package installation failed" ile duser.
echo "Cihazin acilmasi bekleniyor..."
for _ in $(seq 1 60); do
  [ "$("$ADB" shell getprop sys.boot_completed 2>/dev/null | tr -d '\r')" = "1" ] && break
  sleep 5
done
[ "$("$ADB" shell getprop sys.boot_completed 2>/dev/null | tr -d '\r')" = "1" ] \
  || die "Cihaz 5 dakikada acilmadi."

# --- Ag koprusu -------------------------------------------------------------
# `adb reverse`, cihazdaki localhost:PORT'u host'un ayni portuna tunelliyor.
#
# NEDEN 10.0.2.2 DEGIL: emulatorun 10.0.2.2 takma adi host'un loopback'ine
# gider ama Windows'ta gelen baglantiyi guvenlik duvari kesiyor. Belirti
# uygulamanin icinde "No network connection" olur ve hicbir logda gorunmez;
# akis "ana ekran gorunmedi" diye duser ve sebep tamamen saklanir.
# `adb reverse` guvenlik duvarindan etkilenmez ve Linux CI'da da ayni calisir -
# tek yol, iki platform. APK bu yuzden localhost'a derlenir.
"$ADB" reverse "tcp:$API_PORT" "tcp:$API_PORT" >/dev/null \
  || die "adb reverse kurulamadi (port $API_PORT)."
echo "adb reverse: cihaz localhost:$API_PORT -> host localhost:$API_PORT"

# --- APK ve paket adi -------------------------------------------------------
# RELEASE tercih edilir, DEBUG DEGIL: debug APK JS bundle'ini ICERMEZ, calisma
# aninda Metro dev sunucusundan ceker. Metro ayakta degilse uygulama "Unable to
# load script" redbox'i ile acilir ve akislar "ekran gorunmuyor" diye duser.
# Release APK self-contained'dir; Expo sablonu onu debug keystore ile imzaladigi
# icin ek kurulum gerekmez.
APK="$(find "$APP_DIR/android/app/build/outputs/apk/release" -name '*.apk' 2>/dev/null | head -1 || true)"
APK_TIPI="release"
if [ -z "$APK" ]; then
  APK="$(find "$APP_DIR/android/app/build/outputs/apk/debug" -name '*.apk' 2>/dev/null | head -1 || true)"
  APK_TIPI="debug"
fi

if [ -z "${MAESTRO_APP_ID:-}" ]; then
  # Paket adi tespit edilir, sabit yazilmaz: app.json'da `android.package`
  # TANIMSIZ oldugu icin Expo prebuild bugun `com.anonymous.<slug>` uretiyor;
  # gercek paket adi belirlendigi gun bu script'in sessizce yanlis uygulamayi
  # aramasi istenmez. Expo sablonu TEK tirnak kullanir; iki tirnak tipi de kabul.
  if [ -f "$APP_DIR/android/app/build.gradle" ]; then
    MAESTRO_APP_ID="$(sed -nE "s/.*applicationId +['\"]([^'\"]+)['\"].*/\1/p" "$APP_DIR/android/app/build.gradle" 2>/dev/null | head -1 || true)"
  fi
  [ -n "${MAESTRO_APP_ID:-}" ] || die "Paket adi tespit edilemedi. MAESTRO_APP_ID verin."
fi

if [ -z "${E2E_SKIP_INSTALL:-}" ]; then
  [ -n "$APK" ] || die "APK yok. Once derleyin:
    cd $APP_DIR
    EXPO_PUBLIC_API_URL=http://localhost:$API_PORT npx expo prebuild --platform android --no-install
    cd android && ./gradlew assembleRelease -PreactNativeArchitectures=x86_64"

  if [ "$APK_TIPI" = "debug" ]; then
    echo "UYARI: yalniz DEBUG APK bulundu. Debug APK JS bundle'ini icermez ve" >&2
    echo "       Metro dev sunucusu olmadan acilmaz. Onerilen: assembleRelease." >&2
  fi

  echo "APK kuruluyor ($APK_TIPI): $APK"
  "$ADB" install -r "$APK" >/dev/null
fi

# --- API erisilebilirligi ---------------------------------------------------
# Host'tan kontrol edilir: Android imajinda curl YOK, `adb shell curl` her zaman
# basarisiz olur ve sahte uyari uretir. adb reverse kurulduktan sonra host'ta
# saglikli olan uc cihazdan da sagliklidir.
if ! curl -sf -o /dev/null -m 5 "http://localhost:$API_PORT/health"; then
  die "API ayakta degil: http://localhost:$API_PORT/health
    Stack'i kaldirin:  source scripts/e2e-stack.sh"
fi

# --- Slug -------------------------------------------------------------------
# Ortamdan gelirse ona saygi duyulur (CI tek slug'i iki job'a da verir),
# yoksa Playwright'in biraktigi dosyadan okunur.
if [ -z "${E2E_EXPECTED_CATEGORY_SLUG:-}" ]; then
  [ -f "$SLUG_FILE" ] \
    || die "Slug yok. Once admin katmanini kosturun: (cd e2e/admin && npx playwright test)"
  E2E_EXPECTED_CATEGORY_SLUG="$(cat "$SLUG_FILE")"
fi

# Varsayilanlar SupabaseTestIdentity.ClientEmail/ClientPassword ile AYNI olmali.
# Bu hesabi .NET E2E testleri uretir; scripts/e2e-stack.sh onlari cagiriyor.
E2E_CLIENT_EMAIL="${E2E_CLIENT_EMAIL:-e2e-client@psikoal.test}"
E2E_CLIENT_PASSWORD="${E2E_CLIENT_PASSWORD:-E2eClientPass123!}"

echo
echo "Maestro kosuluyor:"
echo "  appId : $MAESTRO_APP_ID"
echo "  slug  : $E2E_EXPECTED_CATEGORY_SLUG"
echo "  hesap : $E2E_CLIENT_EMAIL"
echo

mkdir -p e2e/.artifacts

# Degiskenler `-e` ile GECIRILIR, export YETMEZ. Maestro export edilmis kabuk
# degiskenlerini akislardaki ${VAR} yerine koymuyor; yerine literal "undefined"
# yaziyor. Belirti "giris basarisiz" olur ve kimlik hatasi sanilir - gercekte
# forma "undefined" yazilmistir. Bu ekranda gorulerek teshis edildi.
maestro test e2e/mobile/flows \
  -e "MAESTRO_APP_ID=$MAESTRO_APP_ID" \
  -e "E2E_CLIENT_EMAIL=$E2E_CLIENT_EMAIL" \
  -e "E2E_CLIENT_PASSWORD=$E2E_CLIENT_PASSWORD" \
  -e "E2E_EXPECTED_CATEGORY_SLUG=$E2E_EXPECTED_CATEGORY_SLUG" \
  --format junit \
  --output e2e/.artifacts/maestro-report.xml
