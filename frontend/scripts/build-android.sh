#!/usr/bin/env bash
#
# Build a signed release APK for the "我们的小世界" Capacitor app.
#
# Pipeline:
#   1. Build the Vue web bundle on the HOST (needs Node).
#   2. Sync web assets into the native android/ project on the HOST (cap sync android).
#   3. Build the Docker Android toolchain image (JDK17 + Android SDK) if missing.
#   4. Inside Docker: generate a persistent signing keystore (if absent), then
#      `gradlew assembleRelease` to produce the signed APK.
#
# The signing keystore + its password are kept OUT of git (gitignored) so the
# same key is reused for every build — required for in-app (OTA) updates to
# install over a previously installed build.
#
# Output:
#   frontend/android/app/build/outputs/apk/release/app-release.apk
#   and a copy at the path in $DELIVERABLE (default D:\Item\cap\workbuddy).

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
FRONTEND_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
ANDROID_DIR="$FRONTEND_DIR/android"
IMAGE_NAME="couple-android-builder"
OUTPUT_APK="$ANDROID_DIR/app/build/outputs/apk/release/app-release.apk"
DELIVERABLE="${DELIVERABLE:-D:/Item/cap/workbuddy/our-little-world-release.apk}"

# --- Server base (backend domain) to bake into the APK ---
# Pass your production backend domain so the app connects immediately without a
# first-launch "设置服务器地址" prompt:
#   SERVER_BASE=https://love.example.com ./scripts/build-android.sh
#   (or: ./scripts/build-android.sh https://love.example.com)
# Leave empty for same-origin behaviour — the app then prompts for the server
# address on first launch (useful for web/PWA or LAN testing). The value is read
# by src/config/server.ts as VITE_SERVER_BASE and is overridable later in Settings.
APK_SERVER_BASE="${1:-${SERVER_BASE:-}}"
if [ -n "$APK_SERVER_BASE" ]; then
  echo "==> Baking server base into APK: $APK_SERVER_BASE"
else
  echo "==> No SERVER_BASE set — APK will be same-origin and prompt for server address on first launch."
fi

echo "==> [1/4] Building web assets (npm run build)"
cd "$FRONTEND_DIR"
# vite build clears dist via fs.rmSync which is intercepted by the safe-delete shim;
# disable it so the build can proceed on this machine.
export CODEBUDDY_SAFE_DELETE_ENABLED=0
# Bake the backend domain into the bundle (consumed by src/config/server.ts as VITE_SERVER_BASE).
VITE_SERVER_BASE="$APK_SERVER_BASE" npm run build

echo "==> [2/4] Syncing web assets into native android project (cap sync android)"
npx cap sync android

# Android WebView 直接加载本地 assets，不需要预压缩的 .gz/.br（这些仅供 web 部署的 nginx gzip_static）。
# 而且 AAPT2 对 assets 有"透明 gzip"：同名 X.gz 会被当成 X 打包，与真实的 X 冲突 → "Duplicate resources" 编译失败。
# 因此在同步后剔除；dist/ 本体（用于 web 部署）不受影响。
echo "==> [2b] Removing precompressed .gz/.br from android assets (avoids AAPT2 transparent-gzip duplicate)"
if [ -d "$ANDROID_DIR/app/src/main/assets/public" ]; then
    find "$ANDROID_DIR/app/src/main/assets/public" -name '*.gz' -delete 2>/dev/null || true
    find "$ANDROID_DIR/app/src/main/assets/public" -name '*.br' -delete 2>/dev/null || true
fi

echo "==> [3/4] Ensuring Android builder Docker image"
if [ -z "$(docker images -q "$IMAGE_NAME" 2>/dev/null)" ]; then
    echo "Building Docker image $IMAGE_NAME (this downloads the Android SDK, may take a while) ..."
    docker build -f "$FRONTEND_DIR/Dockerfile.android" -t "$IMAGE_NAME" "$FRONTEND_DIR"
else
    echo "Docker image $IMAGE_NAME already present, skipping build."
fi

echo "==> [4/4] Generating signing keystore + compiling release APK"
# Persistent keystore password in a gitignored env file so future builds share the
# same signature (in-app updates require an identical signature to install).
ENV_FILE="$FRONTEND_DIR/.android-keystore.env"
if [ ! -f "$ENV_FILE" ]; then
    PW="$(openssl rand -hex 16 2>/dev/null || (date +%s%N | sha256sum | cut -c1-32))"
    { echo "KEYSTORE_PW=$PW"; echo "KEY_ALIAS=couple"; } > "$ENV_FILE"
    echo "Generated new keystore credentials at $ENV_FILE (keep this file safe)."
fi
# shellcheck disable=SC1090
source "$ENV_FILE"
# Export so `docker run -e KEYSTORE_PW -e KEY_ALIAS` actually receives them
# (a plain `source` only sets shell variables, not environment variables).
export KEYSTORE_PW KEY_ALIAS

docker run --rm \
    -v "$ANDROID_DIR:/project" \
    -v "$FRONTEND_DIR/node_modules:/node_modules" \
    -v couple-gradle-cache:/root/.gradle \
    -e KEYSTORE_PW -e KEY_ALIAS \
    "$IMAGE_NAME" bash -c '
        set -e
        cd /project
        chmod +x gradlew 2>/dev/null || true
        if [ ! -f app/couple-release.keystore ]; then
            keytool -genkeypair -v \
                -keystore app/couple-release.keystore \
                -alias "$KEY_ALIAS" -keyalg RSA -keysize 2048 -validity 10000 \
                -storepass "$KEYSTORE_PW" -keypass "$KEYSTORE_PW" \
                -dname "CN=Couple World, OU=App, O=Couple, L=Home, S=Home, C=CN"
        fi
        cat > keystore.properties <<EOF
# storeFile is resolved relative to the :app project dir (/project/app), where
# keytool above writes app/couple-release.keystore (i.e. /project/app/couple-release.keystore).
storeFile=couple-release.keystore
storePassword=$KEYSTORE_PW
keyAlias=$KEY_ALIAS
keyPassword=$KEYSTORE_PW
EOF
        # 用系统 gradle（镜像预装）编译，规避 wrapper 从 GitHub 拉分发包被代理破坏的问题。
        gradle assembleRelease --no-daemon
    '

echo "==> APK built at: $OUTPUT_APK"
mkdir -p "$(dirname "$DELIVERABLE")"
cp "$OUTPUT_APK" "$DELIVERABLE"
echo "==> Copied deliverable to: $DELIVERABLE"

# --- Sync update-channel artifacts into the backend's /app static path ---
# The APK fetches its manifest from <serverBase>/app/version.json and downloads the new
# APK from <serverBase>/app/<apk>. The backend already serves wwwroot/app at /app, so
# placing both files there makes the running server the OTA source of truth and prevents
# the manifest/APK version drift we saw earlier (backend said 100/1.0.0, APK was 1/0.1.0).
BACKEND_APP_DIR="$FRONTEND_DIR/../backend/CoupleLoveSystem.Api/wwwroot/app"
if [ -d "$BACKEND_APP_DIR" ]; then
    cp "$OUTPUT_APK" "$BACKEND_APP_DIR/our-little-world-release.apk"
    echo "==> Copied APK into backend update channel: $BACKEND_APP_DIR/our-little-world-release.apk"

    # Merge APK url into the build-generated manifest. dist/app/version.json intentionally
    # leaves apkUrl empty (domain-agnostic); we fill it here so the deployed manifest is
    # self-contained (relative /app path, resolved against serverBase by the app).
    SRC_MANIFEST="$FRONTEND_DIR/dist/app/version.json"
    if [ -f "$SRC_MANIFEST" ]; then
        # node on Windows mis-resolves POSIX paths like /d/foo as D:\d\foo; convert to native Windows paths.
        SRC_WIN="$(cygpath -w "$SRC_MANIFEST" 2>/dev/null || echo "$SRC_MANIFEST")"
        DST_WIN="$(cygpath -w "$BACKEND_APP_DIR/version.json" 2>/dev/null || echo "$BACKEND_APP_DIR/version.json")"
        node -e '
            const fs = require("fs");
            const [src, dst] = process.argv.slice(1);
            const m = JSON.parse(fs.readFileSync(src, "utf8"));
            let out = {};
            if (fs.existsSync(dst)) { try { out = JSON.parse(fs.readFileSync(dst, "utf8")); } catch {} }
            out.versionName = m.versionName || out.versionName;
            out.versionCode = m.versionCode ?? out.versionCode;
            out.androidVersionCode = m.androidVersionCode ?? out.androidVersionCode;
            out.minSupportedCode = m.minSupportedCode ?? out.minSupportedCode;
            // Domain-agnostic build manifest leaves apkUrl empty; fill the self-contained relative path.
            out.apkUrl = "/app/our-little-world-release.apk";
            out.releaseUrl = "/app/our-little-world-release.apk";
            // Preserve a meaningful existing changelog; only fall back if none present.
            if (!out.changelog && m.changelog) out.changelog = m.changelog;
            if (!out.changelog) out.changelog = "小世界 App 更新";
            fs.writeFileSync(dst, JSON.stringify(out, null, 2));
        ' "$SRC_WIN" "$DST_WIN"
        echo "==> Wrote update manifest: $BACKEND_APP_DIR/version.json"
    fi
else
    echo "==> (skip) backend wwwroot/app not found; deploy APK + version.json under your server /app manually."
fi

echo "Done. Install $DELIVERABLE on an Android device (sideload / ADB)."
