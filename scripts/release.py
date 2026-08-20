#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
发版脚本：一键 bump 版本并推送，自动触发 iOS / Android 云构建。

用法：
    python scripts/release.py 1.4                          # 常规发版（iOS versionCode+1，Android 保持）
    python scripts/release.py 1.4 --bump-android           # Android 本期也要重装（androidVersionCode+1）
    python scripts/release.py 1.4 --changelog "更新说明"
    python scripts/release.py 1.4 --dry-run                # 只预览改动，不写文件不提交

行为：
    1. 读取部署版 version.json 与 mobile/android/app/build.gradle 当前版本
    2. versionCode = 当前 iOS versionCode + 1；--bump-android 时 androidVersionCode 同步 +1
    3. 写回 version.json（versionName / versionCode / androidVersionCode / changelog / apkUrl）
    4. 写回 build.gradle（versionCode / versionName）
    5. git add + commit + push（push 触发 .github/workflows 的 iOS/Android 云构建 + CI）

版本约定：
    - iOS 壳含原生启动屏等资源 → 每次发版 versionCode +1 提示重装
    - Android 为远程模式，仅原生壳改动时才需 --bump-android
"""
import argparse
import json
import re
import subprocess
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent          # D:\Code\My_vscode\couple-love-system
VERSION_JSON = Path(r"D:\Docker\couple-love-system\app\version.json")  # 部署目录（不入库）
BUILD_GRADLE = REPO / "mobile" / "android" / "app" / "build.gradle"
GITHUB_REPO = "JJAssassin/couple_system"


def read_version_json() -> dict:
    if not VERSION_JSON.exists():
        raise SystemExit(f"[!] 找不到 {VERSION_JSON}，请确认部署目录存在")
    return json.loads(VERSION_JSON.read_text(encoding="utf-8"))


def read_gradle() -> tuple[int, str]:
    txt = BUILD_GRADLE.read_text(encoding="utf-8")
    vc = re.search(r"versionCode\s+(\d+)", txt)
    vn = re.search(r'versionName\s+"([^"]+)"', txt)
    if not vc or not vn:
        raise SystemExit("[!] build.gradle 里读不到 versionCode/versionName")
    return int(vc.group(1)), vn.group(1)


def write_gradle(version_code: int, version_name: str) -> None:
    txt = BUILD_GRADLE.read_text(encoding="utf-8")
    txt = re.sub(r"versionCode\s+\d+", f"versionCode {version_code}", txt, count=1)
    txt = re.sub(r'versionName\s+"[^"]+"', f'versionName "{version_name}"', txt, count=1)
    BUILD_GRADLE.write_text(txt, encoding="utf-8")


def main() -> int:
    p = argparse.ArgumentParser(description="情侣小世界一键发版")
    p.add_argument("version", help="新版本号，如 1.4（可带 v 前缀）")
    p.add_argument("--bump-android", action="store_true", help="本期 Android 壳也要重装（androidVersionCode 同步 +1）")
    p.add_argument("--changelog", default="", help="写入 version.json 的更新说明（\\n 换行）")
    p.add_argument("--dry-run", action="store_true", help="只预览改动，不写文件、不提交")
    args = p.parse_args()

    version = args.version.lstrip("v")
    vj = read_version_json()
    g_vc, g_vn = read_gradle()

    new_code = vj["versionCode"] + 1                      # iOS 提示重装的 code
    android_code = vj.get("androidVersionCode", vj["versionCode"])
    if args.bump_android:
        android_code += 1
        new_code = max(new_code, android_code)            # 保证 versionCode >= androidVersionCode

    apk_url = f"https://github.com/{GITHUB_REPO}/releases/download/android-v{version}/app-release.apk"
    new_vj = {
        "versionName": version,
        "versionCode": new_code,
        "androidVersionCode": android_code,
        "changelog": args.changelog or vj.get("changelog", ""),
        "apkUrl": apk_url,
        "releaseUrl": vj.get("releaseUrl", f"https://github.com/{GITHUB_REPO}/releases/latest"),
        "minSupportedCode": vj.get("minSupportedCode", 1),
    }

    print(f"当前: version.json={vj['versionName']}(code {vj['versionCode']}/android {vj.get('androidVersionCode')})  "
          f"build.gradle={g_vn}({g_vc})")
    print(f"目标: v{version}  versionCode {new_code}  androidVersionCode {android_code}")
    print(f"apkUrl: {apk_url}")
    if args.dry_run:
        print("\n[dry-run] 仅预览，未做任何改动")
        return 0

    # 1) version.json（部署目录，格式化中文友好）
    VERSION_JSON.write_text(json.dumps(new_vj, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"[ok] 已写 {VERSION_JSON}")

    # 2) build.gradle
    write_gradle(new_code, version)
    print(f"[ok] 已写 {BUILD_GRADLE.relative_to(REPO)}")

    # 3) git 提交推送（触发 iOS/Android workflow）
    subprocess.run(["git", "add", str(BUILD_GRADLE.relative_to(REPO))], cwd=REPO, check=True)
    msg = f"chore(release): bump v{version}（code {new_code}/android {android_code}）"
    subprocess.run(["git", "commit", "-m", msg], cwd=REPO, check=True)
    subprocess.run(["git", "push", "origin", "master"], cwd=REPO, check=True)
    print("[ok] 已推送，iOS/Android 云构建自动触发中")
    print(f"提醒: {VERSION_JSON} 在部署目录（不入库），容器挂载的是宿主机文件，无需重建即生效")
    return 0


if __name__ == "__main__":
    sys.exit(main())
